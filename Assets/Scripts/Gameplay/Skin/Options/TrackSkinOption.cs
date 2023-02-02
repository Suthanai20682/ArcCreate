// <auto-generated> to shut up linter
using System.IO;
using ArcCreate.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ArcCreate.Gameplay.Skin
{
    [CreateAssetMenu(fileName = "Track", menuName = "Skin Option/Track")]
    public class TrackSkinOption : ScriptableObject
    {
        public string Name;
        public Sprite TrackSkin;
        public Sprite TrackExtraSkin;

        public Sprite replacedTrack;
        public Sprite replacedTrackExtra;

        internal async UniTask LoadExternalSkin()
        {
            var track = await Importer.GetSprite(Path.Combine(Values.SkinFolderPath, "Track", TrackSkin.name + ".png"));
            if (track != null)
            {
                replacedTrack = TrackSkin;
                TrackSkin = track;
            }

            var trackExtra = await Importer.GetSprite(Path.Combine(Values.SkinFolderPath, "Track", TrackExtraSkin.name + ".png"));
            if (trackExtra != null)
            {
                replacedTrackExtra = TrackExtraSkin;
                TrackExtraSkin = trackExtra;
            }
        }

        internal void UnloadExternalSkin()
        {
            if (replacedTrack != null)
            {
                Destroy(TrackSkin);
                TrackSkin = replacedTrack;
                replacedTrack = null;
            }

            if (replacedTrackExtra != null)
            {
                Destroy(TrackExtraSkin);
                TrackExtraSkin = replacedTrackExtra;
                replacedTrackExtra = null;
            }
        }
    }
}