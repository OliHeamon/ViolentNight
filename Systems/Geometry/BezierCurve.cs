using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ViolentNight.Systems.Geometry;

public class BezierCurve
{
    public static List<Vector2> GetPoints(Vector2[] controlPoints, int totalPoints)
    {
        float perStep = 1f / totalPoints;

        List<Vector2> points = [];

        for (float step = 0f; step <= 1f; step += perStep)
        {
            float t = MathHelper.Clamp(step, 0, 1);

            points.Add(GetPoint(controlPoints, t));
        }

        return points;
    }

    private static Vector2 GetPoint(Vector2[] points, float t)
    {
        while (points.Length > 2)
        {
            Vector2[] nextPoints = new Vector2[points.Length - 1];

            for (int k = 0; k < points.Length - 1; k++)
            {
                nextPoints[k] = Vector2.Lerp(points[k], points[k + 1], t);
            }

            points = nextPoints;
        }

        if (points.Length <= 1)
            return Vector2.Zero;

        return Vector2.Lerp(points[0], points[1], t);
    }
}
