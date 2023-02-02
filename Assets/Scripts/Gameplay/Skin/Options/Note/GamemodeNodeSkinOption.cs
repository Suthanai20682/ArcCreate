// <auto-generated> to shut up linter
using System.IO;
using ArcCreate.Gameplay.Data;
using ArcCreate.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ArcCreate.Gameplay.Skin
{
    public abstract class GamemodeNoteSkinOption : ScriptableObject, INoteSkinProvider
    {
        public string Name;
        public Material ArcTapSfxSkin;
        public Color ConnectionLineColor;
        public Mesh ArcTapMesh;
        public Mesh ArcTapSfxMesh;

        private Texture replacedArcTapSfxTexture;

        public abstract (Mesh mesh, Material material) GetArcTapSkin(ArcTap note);
        public abstract (Sprite normal, Sprite highlight) GetHoldSkin(Hold note);
        public abstract Sprite GetTapSkin(Tap note);
        public abstract Sprite GetArcCapSprite(Arc arc);

        internal virtual async UniTask LoadExternalSkin()
        {
            var arctapsfx = await Importer.GetTexture(Path.Combine(Values.SkinFolderPath, "Note", ArcTapSfxSkin.mainTexture.name + ".png"));
            if (arctapsfx != null)
            {
                replacedArcTapSfxTexture = ArcTapSfxSkin.mainTexture;
                ArcTapSfxSkin.mainTexture = arctapsfx;
            }
        }

        internal virtual void UnloadExternalSkin()
        {
            if (replacedArcTapSfxTexture != null)
            {
                Destroy(ArcTapSfxSkin.mainTexture);
                ArcTapSfxSkin.mainTexture = replacedArcTapSfxTexture;
            }
        }
    }
}