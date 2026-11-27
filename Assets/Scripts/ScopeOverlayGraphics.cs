using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScopeOverlayGraphic : MaskableGraphic
{
    [Header("조준경 모양")]
    [SerializeField, Range(0.3f, 0.95f)]
    private float diameterRatio = 0.85f;

    [SerializeField, Range(32, 256)]
    private int circleSegments = 128;

    [SerializeField, Min(0.5f)]
    private float reticleThickness = 2f;

    protected override void Awake()
    {
        base.Awake();

        // 사격이나 UI 클릭을 가로막지 않는다.
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;

        if (rect.width <= 0f || rect.height <= 0f)
            return;

        Vector2 center = rect.center;

        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;

        float radius =
            Mathf.Min(rect.width, rect.height)
            * Mathf.Clamp(diameterRatio, 0.3f, 0.95f)
            * 0.5f;

        int segments = Mathf.Clamp(circleSegments, 32, 256);

        List<float> angles = new List<float>(segments + 4);

        for (int i = 0; i < segments; i++)
        {
            angles.Add(i * Mathf.PI * 2f / segments);
        }

        // 화면의 네 모서리까지 빈틈없이 덮기 위한 각도.
        float cornerAngle = Mathf.Atan2(halfHeight, halfWidth);

        angles.Add(cornerAngle);
        angles.Add(Mathf.PI - cornerAngle);
        angles.Add(Mathf.PI + cornerAngle);
        angles.Add(Mathf.PI * 2f - cornerAngle);
        angles.Sort();

        for (int i = 0; i < angles.Count; i++)
        {
            float angleA = angles[i];
            float angleB = angles[(i + 1) % angles.Count];

            Vector2 directionA = new Vector2(
                Mathf.Cos(angleA),
                Mathf.Sin(angleA)
            );

            Vector2 directionB = new Vector2(
                Mathf.Cos(angleB),
                Mathf.Sin(angleB)
            );

            Vector2 innerA = center + directionA * radius;
            Vector2 innerB = center + directionB * radius;

            Vector2 outerA = center + GetRectEdge(
                directionA, halfWidth, halfHeight
            );

            Vector2 outerB = center + GetRectEdge(
                directionB, halfWidth, halfHeight
            );

            // 원 안쪽은 그리지 않고 바깥 영역만 검게 채운다.
            AddQuad(vh, innerA, outerA, outerB, innerB);
        }

        float halfLine = Mathf.Min(
            Mathf.Max(0.5f, reticleThickness) * 0.5f,
            radius * 0.1f
        );

        // 가로 십자선
        AddQuad(
            vh,
            center + new Vector2(-radius, -halfLine),
            center + new Vector2(-radius, halfLine),
            center + new Vector2(radius, halfLine),
            center + new Vector2(radius, -halfLine)
        );

        // 세로 십자선
        AddQuad(
            vh,
            center + new Vector2(-halfLine, -radius),
            center + new Vector2(-halfLine, radius),
            center + new Vector2(halfLine, radius),
            center + new Vector2(halfLine, -radius)
        );
    }

    private Vector2 GetRectEdge(
        Vector2 direction,
        float halfWidth,
        float halfHeight)
    {
        float distanceX = Mathf.Abs(direction.x) > 0.00001f
            ? halfWidth / Mathf.Abs(direction.x)
            : float.PositiveInfinity;

        float distanceY = Mathf.Abs(direction.y) > 0.00001f
            ? halfHeight / Mathf.Abs(direction.y)
            : float.PositiveInfinity;

        return direction * Mathf.Min(distanceX, distanceY);
    }

    private void AddQuad(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d)
    {
        int start = vh.currentVertCount;
        Color32 black = new Color32(0, 0, 0, 255);

        vh.AddVert(new Vector3(a.x, a.y, 0f), black, Vector2.zero);
        vh.AddVert(new Vector3(b.x, b.y, 0f), black, Vector2.zero);
        vh.AddVert(new Vector3(c.x, c.y, 0f), black, Vector2.zero);
        vh.AddVert(new Vector3(d.x, d.y, 0f), black, Vector2.zero);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}