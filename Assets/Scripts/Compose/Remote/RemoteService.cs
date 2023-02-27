using System;
using System.Net;
using System.Text;
using ArcCreate.Compose.Timeline;
using ArcCreate.Remote.Common;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Compose.Remote
{
    public class RemoteService : MonoBehaviour, IProtocol
    {
        [Header("Icons")]
        [SerializeField] private GameObject idleIndicator;
        [SerializeField] private GameObject broadcastingIndicator;
        [SerializeField] private GameObject connectedIndicator;
        [SerializeField] private GameObject sendingIndicator;

        [Header("Buttons")]
        [SerializeField] private Button startBroadcastButton;
        [SerializeField] private Button abortBroadcastButton;
        [SerializeField] private Button broadcastAgainButton;
        [SerializeField] private Button stopSessionButton;

        [Header("Others")]
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private RemoteDataSender remoteDataSender;
        [SerializeField] private Marker remoteCurrentTiming;
        [SerializeField] private RectTransform layout;

        private string code = "------";
        private readonly BroadcastSender broadcastSender = new BroadcastSender(Ports.Gameplay);
        private MessageChannel channel;
        private FileHoster fileHoster;
        private RemoteState state = RemoteState.Idle;

        private enum RemoteState
        {
            Idle,
            Broadcasting,
            Connected,
            Sending,
        }

        public bool IsConnected => state == RemoteState.Connected;

        public void Process(RemoteControl control, byte[] message)
        {
            switch (control)
            {
                case RemoteControl.CurrentTiming:
                    SetRemoteMarkerTiming(BitConverter.ToInt32(message, 0)).Forget();
                    break;
                case RemoteControl.Abort:
                    OnTargetDisconnect().Forget();
                    break;
            }
        }

        private void SetState(RemoteState state)
        {
            this.state = state;

            idleIndicator.SetActive(state == RemoteState.Idle);
            broadcastingIndicator.SetActive(state == RemoteState.Broadcasting);
            connectedIndicator.SetActive(state == RemoteState.Connected);
            sendingIndicator.SetActive(state == RemoteState.Sending);

            startBroadcastButton.gameObject.SetActive(state == RemoteState.Idle);
            abortBroadcastButton.gameObject.SetActive(state == RemoteState.Broadcasting);
            broadcastAgainButton.gameObject.SetActive(state == RemoteState.Broadcasting);
            stopSessionButton.gameObject.SetActive(state == RemoteState.Connected);

            switch (state)
            {
                case RemoteState.Idle:
                    descriptionText.text = I18n.S("Remote.Description.Idle");
                    break;
                case RemoteState.Broadcasting:
                    descriptionText.text = I18n.S("Remote.Description.Broadcasting", code);
                    break;
                case RemoteState.Connected:
                case RemoteState.Sending:
                    descriptionText.text = I18n.S("Remote.Description.Connected", channel.IPAddress, channel.SendToPort);
                    break;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
        }

        private void Awake()
        {
            startBroadcastButton.onClick.AddListener(StartBroadcast);
            abortBroadcastButton.onClick.AddListener(StopSession);
            broadcastAgainButton.onClick.AddListener(BroadcastAgain);
            stopSessionButton.onClick.AddListener(StopSession);
            I18n.OnLocaleChanged += OnLocaleChanged;
            SetState(RemoteState.Idle);
        }

        private void OnDestroy()
        {
            startBroadcastButton.onClick.RemoveListener(StartBroadcast);
            abortBroadcastButton.onClick.RemoveListener(StopSession);
            broadcastAgainButton.onClick.RemoveListener(BroadcastAgain);
            stopSessionButton.onClick.RemoveListener(StopSession);
            I18n.OnLocaleChanged -= OnLocaleChanged;
            Dispose();
        }

        private void OnLocaleChanged()
        {
            SetState(state);
        }

        private void StartBroadcast()
        {
            code = GenerateRandomMessage();
            broadcastSender.Broadcast(code);

            DisposeAny();
            SetState(RemoteState.Broadcasting);

            channel = new MessageChannel(this);
            channel.OnError += OnChannelError;
            channel.SetupListener(Ports.Compose);
            channel.WaitForConnection(code, OnClientConnect);

            Debug.Log($"Compose: Sent broadcast to port {Ports.Gameplay} {code}");
            Debug.Log($"Compose: Start listening on port {Ports.Compose}");
        }

        private void BroadcastAgain()
        {
            code = GenerateRandomMessage();
            broadcastSender.Broadcast(code);
            channel.UpdateCode(code);
            Debug.Log($"Compose: Sent broadcast to port {Ports.Gameplay} {code}");
            SetState(RemoteState.Broadcasting);
        }

        private async UniTask StartSession(IPAddress ipAddress)
        {
            await UniTask.SwitchToMainThread();
            DisposeAny();
            fileHoster = new FileHoster(Ports.HttpCompose, remoteDataSender);
            await channel.SetupSender(ipAddress, Ports.Gameplay, code);
            channel.SendMessage(RemoteControl.StartConnection, Encoding.ASCII.GetBytes(code));

            while (!fileHoster.IsRunning)
            {
                await UniTask.NextFrame();
            }

            remoteDataSender.SetTarget(channel);

            SetState(RemoteState.Connected);
            Debug.Log($"Compose: Started session with {ipAddress}:{Ports.Gameplay} {code}");
        }

        private void StopSession()
        {
            Dispose();
            remoteDataSender.RemoveTarget();
            SetState(RemoteState.Idle);

            Debug.Log($"Compose: Stopped session");
        }

        private void Dispose()
        {
            channel?.SendMessage(RemoteControl.Abort, System.Text.Encoding.ASCII.GetBytes("From Compose"));
            broadcastSender.Broadcast(Constants.Abort);
            DisposeAny();
        }

        private async UniTask OnTargetDisconnect()
        {
            await UniTask.SwitchToMainThread();
            DisposeAny();
            SetState(RemoteState.Idle);
            broadcastSender.Broadcast(Constants.Abort);
            remoteDataSender.RemoveTarget();

            Debug.Log($"Compose: Target disconnected");
            Services.Popups.Notify(Popups.Severity.Info, I18n.S("Remote.State.TargetDisconnected.Compose"));
        }

        private void OnChannelError()
        {
            OnTargetDisconnect().Forget();
        }

        private string GenerateRandomMessage()
        {
            int code = UnityEngine.Random.Range(0, 999999);
            return code.ToString("D6");
        }

        private void OnClientConnect(IPAddress ipAddress)
        {
            StartSession(ipAddress).Forget();
        }

        private void DisposeAny()
        {
            if (channel?.IsRunning ?? false)
            {
                channel.Dispose();
                channel.OnError -= OnChannelError;
                channel = null;
            }

            if (fileHoster?.IsRunning ?? false)
            {
                fileHoster.Dispose();
                fileHoster = null;
            }
        }

        private async UniTask SetRemoteMarkerTiming(int timing)
        {
            await UniTask.SwitchToMainThread();
            if (!remoteCurrentTiming.IsDragging)
            {
                remoteCurrentTiming.SetTiming(timing);
            }
        }
    }
}