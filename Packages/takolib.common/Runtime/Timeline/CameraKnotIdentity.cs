using System;
using UnityEngine;
using UnityEngine.Splines;

namespace TakoLib.Common.Timeline
{
    // Knotの識別子をSpline内に保存し、シリアライズとUndoの対象にする。
    public static class CameraKnotIdentity
    {
        public const string DATA_KEY = "TakoLib.CameraShot.KnotId";

        public static int GetOrCreate(Spline spline, int knotIndex)
        {
            if (knotIndex < 0 || knotIndex >= spline.Count)
                throw new ArgumentOutOfRangeException(nameof(knotIndex));
            var data = spline.GetOrCreateIntData(DATA_KEY);
            data.PathIndexUnit = PathIndexUnit.Knot;
            for (int i = 0; i < data.Count; i++)
                if (Mathf.Abs(data[i].Index - knotIndex) < 0.0001f) return data[i].Value;
            int id;
            do { id = Guid.NewGuid().GetHashCode() & int.MaxValue; }
            while (id == 0 || Find(spline, id) >= 0);
            data.Add(knotIndex, id);
            return id;
        }

        public static int Find(Spline spline, int id)
        {
            if (id == 0 || spline == null || !spline.TryGetIntData(DATA_KEY, out var data)) return -1;
            for (int i = 0; i < data.Count; i++)
            {
                var point = data[i];
                int index = Mathf.RoundToInt(point.Index);
                if (point.Value == id && index >= 0 && index < spline.Count &&
                    Mathf.Abs(point.Index - index) < 0.0001f) return index;
            }
            return -1;
        }
    }
}
