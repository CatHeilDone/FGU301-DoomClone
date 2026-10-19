using UnityEngine;
using UnityEngine.UI;

public class UIGradient : BaseMeshEffect
{
    public enum Direction { Vertical, Horizontal }
    public Direction direction = Direction.Vertical;
    public Color topOrLeft = Color.white;
    public Color bottomOrRight = Color.gray;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        UIVertex v = new UIVertex();
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float p = direction == Direction.Vertical ? v.position.y : v.position.x;
            min = Mathf.Min(min, p);
            max = Mathf.Max(max, p);
        }
        float size = max - min;
        if (size <= 0) return;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float p = direction == Direction.Vertical ? v.position.y : v.position.x;
            float t = (p - min) / size;
            Color c = direction == Direction.Vertical
                ? Color.Lerp(bottomOrRight, topOrLeft, t)
                : Color.Lerp(topOrLeft, bottomOrRight, t);
            v.color = (Color32)(c * (Color)v.color);
            vh.SetUIVertex(v, i);
        }
    }
}