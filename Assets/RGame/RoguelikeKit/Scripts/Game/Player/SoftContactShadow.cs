using UnityEngine;
namespace RGame.RoguelikeKit
{
    // One shared procedural ellipse. Keep the existing renderer so death fading
    // continues to use PlayerHit's serialized reference.
    public sealed class SoftContactShadow : MonoBehaviour
    {
        static Sprite shape;
        static Texture2D texture;
        static SoftContactShadow()
        {
            Application.quitting+=ResetShared;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload+=ResetShared;
#endif
        }
        SpriteRenderer body, shadow;
        public static void Configure(SpriteRenderer target,SpriteRenderer actor)
        {
            if(target==null||actor==null)return;
            if(shape==null)
            {
                texture=new Texture2D(64,32,TextureFormat.RGBA32,false){name="Soft contact shadow",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                var pixels=new Color32[64*32];
                for(int y=0;y<32;y++)for(int x=0;x<64;x++)
                {
                    float dx=(x+.5f)/32f-1,dy=(y+.5f)/16f-1;
                    float a=Mathf.SmoothStep(0,.7f,1-Mathf.Sqrt(dx*dx+dy*dy));
                    pixels[y*64+x]=new Color(1,1,1,a);
                }
                texture.SetPixels32(pixels);texture.Apply(false,true);
                shape=Sprite.Create(texture,new Rect(0,0,64,32),new Vector2(.5f,.5f),64,0,SpriteMeshType.FullRect);
                shape.name="Soft contact ellipse";shape.hideFlags=HideFlags.HideAndDontSave;
            }
            target.sharedMaterial=actor.sharedMaterial;
            target.sprite=shape;target.drawMode=SpriteDrawMode.Simple;target.color=new Color(0,0,0,.22f);
            float width=Mathf.Clamp(actor.sprite!=null?actor.sprite.bounds.size.x*.5f:1.2f,.85f,1.4f);
            target.transform.localScale=new Vector3(width,width*.52f,1);
            var follow=target.GetComponent<SoftContactShadow>()??target.gameObject.AddComponent<SoftContactShadow>();
            follow.body=actor;follow.shadow=target;follow.LateUpdate();
        }
        void LateUpdate()
        {
            if(body==null||shadow==null)return;
            shadow.sortingLayerID=body.sortingLayerID;shadow.sortingOrder=body.sortingOrder-1;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetShared()
        {
            if(shape!=null){if(Application.isPlaying)Destroy(shape);else DestroyImmediate(shape);}
            if(texture!=null){if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
            shape=null;texture=null;
        }
    }
}
