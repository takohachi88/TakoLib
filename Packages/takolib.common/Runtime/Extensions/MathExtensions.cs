using System;
using Unity.Mathematics;
using UnityEngine;

namespace TakoLib.Common.Extensions
{
    public static class MathExtensions
    {
        public static float4 ToFloat4(this Color self) => new(self.r, self.g, self.b, self.a);
        public static float3 ToFloat3(this Color self) => new(self.r, self.g, self.b);
    }
}
