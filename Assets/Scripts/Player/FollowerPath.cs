using System;
using UnityEngine;

public sealed class FollowerPath
{
    private struct Point
    {
        public Vector3 Position;
        public double Distance;
    }

    public struct Cursor
    {
        internal Vector3 Head;
        internal Vector3 Projection;
        internal Vector3 Forward;
        internal float ConnectorLength;
        internal double Distance;
        internal int Segment;
    }

    private const int MaximumPoints = 8192;
    private Point[] points = new Point[128];
    private int start;
    public int Count { get; private set; }
    public Vector3 Newest => Count == 0 ? Vector3.zero : At(Count - 1).Position;

    private Point At(int index) => points[(start + index) % points.Length];

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    public void Clear()
    {
        start = 0;
        Count = 0;
    }

    public bool Append(Vector3 position)
    {
        position = Flat(position);
        double distance = 0d;
        if (Count > 0)
        {
            Point previous = At(Count - 1);
            float length = Vector3.Distance(previous.Position, position);
            if (length < 0.0001f)
                return false;
            distance = previous.Distance + length;
        }

        if (Count == points.Length)
        {
            if (points.Length < MaximumPoints)
            {
                Point[] larger = new Point[Math.Min(points.Length * 2, MaximumPoints)];
                for (int i = 0; i < Count; i++)
                    larger[i] = At(i);
                points = larger;
                start = 0;
            }
            else
            {
                start = (start + 1) % points.Length;
                Count--;
            }
        }

        points[(start + Count) % points.Length] = new Point
        {
            Position = position,
            Distance = distance
        };
        Count++;
        return true;
    }

    public void Trim(float retainedDistance)
    {
        while (Count > 2 && At(Count - 1).Distance - At(1).Distance > retainedDistance)
        {
            start = (start + 1) % points.Length;
            Count--;
        }
    }

    public Vector3[] CopyPoints()
    {
        Vector3[] result = new Vector3[Count];
        for (int i = 0; i < Count; i++)
            result[i] = At(i).Position;
        return result;
    }

    public void Load(Vector3[] positions)
    {
        Clear();
        if (positions == null)
            return;
        foreach (Vector3 position in positions)
            Append(position);
    }

    public Cursor Begin(Vector3 displayedHead, Vector3 forward)
    {
        displayedHead = Flat(displayedHead);
        forward = Flat(forward).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        Cursor cursor = new Cursor
        {
            Head = displayedHead,
            Projection = displayedHead,
            Forward = forward,
            Segment = Math.Max(0, Count - 2)
        };

        if (Count < 2)
            return cursor;

        Point latest = At(Count - 1);
        if (Vector3.Dot(displayedHead - latest.Position, forward) >= 0f)
        {
            cursor.Projection = latest.Position;
            cursor.Distance = latest.Distance;
            cursor.ConnectorLength = Vector3.Distance(displayedHead, latest.Position);
            return cursor;
        }

        float bestDistance = float.PositiveInfinity;
        int oldestCandidate = Math.Max(0, Count - 33);
        for (int i = Count - 2; i >= oldestCandidate; i--)
        {
            Point older = At(i);
            Point newer = At(i + 1);
            Vector3 segment = newer.Position - older.Position;
            float t = Mathf.Clamp01(Vector3.Dot(displayedHead - older.Position, segment) / segment.sqrMagnitude);
            Vector3 projection = Vector3.Lerp(older.Position, newer.Position, t);
            float distance = (displayedHead - projection).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            cursor.Projection = projection;
            cursor.Distance = older.Distance + (newer.Distance - older.Distance) * t;
            cursor.Segment = i;
        }

        cursor.ConnectorLength = Vector3.Distance(displayedHead, cursor.Projection);
        return cursor;
    }


    public Vector3 Behind(ref Cursor cursor, float distance, out Vector3 forward)
    {
        distance = Mathf.Max(0f, distance);
        forward = cursor.Forward;
        if (Count < 2)
            return cursor.Head - forward * distance;

        if (cursor.ConnectorLength > 0.0001f && distance <= cursor.ConnectorLength)
        {
            forward = (cursor.Head - cursor.Projection).normalized;
            return Vector3.Lerp(cursor.Head, cursor.Projection, distance / cursor.ConnectorLength);
        }

        double target = cursor.Distance - (distance - cursor.ConnectorLength);
        while (cursor.Segment > 0 && target < At(cursor.Segment).Distance)
            cursor.Segment--;
        while (cursor.Segment < Count - 2 && target > At(cursor.Segment + 1).Distance)
            cursor.Segment++;

        Point older = At(cursor.Segment);
        Point newer = At(cursor.Segment + 1);
        forward = (newer.Position - older.Position).normalized;
        float t = (float)((target - older.Distance) / (newer.Distance - older.Distance));
        return older.Position + (newer.Position - older.Position) * t;
    }
}
