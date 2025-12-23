namespace TakoLib.Common
{
    public static partial class Defines
    {
        /// <summary>
        /// ベクトル型のサイズはsizeofで取得できないため共通定義。
        /// </summary>
        public static class SizeOf
        {
            public const int FLOAT = sizeof(float);
            public const int FLOAT2 = FLOAT * 2;
            public const int FLOAT3 = FLOAT * 3;
            public const int FLOAT4 = FLOAT * 4;

            public const int FLOAT4X4 = FLOAT * 16;
        }
    }
}
