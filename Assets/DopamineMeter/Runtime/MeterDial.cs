using UnityEngine;
using UnityEngine.UI;

namespace DopamineMeter
{
    /// <summary>Resolution-independent segmented dial; no baked-in numbers or gauge state.</summary>
    public sealed class MeterDial : MaskableGraphic
    {
        public float fraction;
        public Color activeColor = Color.cyan;
        public void Set(float value, Color tint) { fraction = value / 100f; activeColor = tint; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            float outer = Mathf.Min(r.width, r.height) / 2, inner = outer - 8;
            for (int i = 0; i < 80; i++)
            {
                float a = (225 - i * 3.75f) * Mathf.Deg2Rad, b = (225 - i * 3.75f - 2.3f) * Mathf.Deg2Rad;
                Color tint = i < fraction * 80 ? activeColor : new Color(.16f, .19f, .26f, .85f);
                int n = vh.currentVertCount;
                vh.AddVert(new Vector3(Mathf.Cos(a)*inner, Mathf.Sin(a)*inner), tint, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Cos(a)*outer, Mathf.Sin(a)*outer), tint, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Cos(b)*outer, Mathf.Sin(b)*outer), tint, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Cos(b)*inner, Mathf.Sin(b)*inner), tint, Vector2.zero);
                vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
