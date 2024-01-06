#ifndef TAKOLIB_COMMON_INCLUDED
#define TAKOLIB_COMMON_INCLUDED

#define PI_RCP 0.318301
#define PI_TWO_RCP 0.159155

#ifndef UINT_MAX
#define UINT_MAX 0xffffffffu
#endif

#define HASH_MAGIC uint3(0x456789abu, 0x6789ab45u, 0x89ab4567u)
#define HASH_SHIFT uint3(1, 2, 3)

#define COMMON_TIME _Time.y

#define TIME_RIGHT float2(COMMON_TIME, 0)
#define TIME_UP float2(0, COMMON_TIME)


float2 Rotate(float2 uv, float radian)
{
	float2 sinCos = 0;
	sincos(radian, sinCos.x, sinCos.y);

	float2x2 rotationMatrix = float2x2(sinCos.y, -sinCos.x, sinCos.x, sinCos.y);

	return mul(rotationMatrix, uv + 0.5) - 0.5;
}

//xÇÕoffsetÅAyÇÕscale
float2 Polar(float2 uv, float2 radiusSt, float2 thetaSt)
{
    float radius = length(uv) * 2 * radiusSt.y + radiusSt.x;
    float theta = atan2(uv.x, uv.y) / PI * thetaSt.y + thetaSt.x;

    return float2(radius, theta);
}


//XorShiftÇ…ÇÊÇÈçÇïiéøí·ïââ◊Ç»ã^éóóêêîÅB
uint UHash11(uint n)
{
    n ^= (n << 1);
    n ^= (n >> 1);
    n *= HASH_MAGIC.x;
    n ^= (n << 1);
    return n * HASH_MAGIC.x;
}

uint2 UHash22(uint2 n)
{
    n ^= (n.yx << HASH_SHIFT.xy);
    n ^= (n.yx >> HASH_SHIFT.xy);
    n *= HASH_MAGIC.xy;
    n ^= (n.yx << HASH_SHIFT.xy);
    return n * HASH_MAGIC.xy;
}

uint3 UHash33(uint3 n)
{
    n ^= (n.yzx << HASH_SHIFT);
    n ^= (n.yzx >> HASH_SHIFT);
    n *= HASH_MAGIC;
    n ^= (n.yzx << HASH_SHIFT);
    return n * HASH_MAGIC;
}

float Hash11(float f)
{
    uint n = asuint(f);
    return float(UHash11(n)) * rcp(UINT_MAX);
}

float2 Hash22(float2 f)
{
    uint2 n = asuint(f);
    return float2(UHash22(n)) * rcp(UINT_MAX);
}

float3 Hash33(float3 f)
{
    uint3 n = asuint(f);
    return float3(UHash33(n)) * rcp(UINT_MAX);
}

float TriangleWave(float t)
{
    return 1 - abs(frac(t * 0.5) * 2 - 1);
}

float Remap(float input, float2 inMinMax, float2 outMinMax)
{
    return outMinMax.x + (input - inMinMax.x) * (outMinMax.y - outMinMax.x) / (inMinMax.y - inMinMax.x);
}

float2 VoronoiRandom (float2 uv, float offset)
{
    uv = Hash22(uv);
    return float2(sin(uv.y * offset) * 0.5 + 0.5, cos(uv.x * offset) * 0.5 + 0.5);
}

float2 VoronoiRandom2 (float2 uv, float offset)
{
    return float2(sin(uv.y * offset) * 0.5 + 0.5, cos(uv.x * offset) * 0.5 + 0.5);
}

//x: distance field
//yz: cell color
half3 Voronoi(float2 uv, float t)
{
    float2 g = floor(uv);
    float2 f = frac(uv);
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y = -1; y <= 1; y++)
    {
        for(int x = -1; x <= 1; x++)
        {
            float2 lattice = float2(x, y);
            float2 offset = VoronoiRandom(lattice + g, t);
            float d = distance(lattice + offset, f);
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
            }
        }
    }

    return res;
}

half3 Voronoi2(float2 uv, float t)
{
    float2 g = floor(uv);
    float2 f = frac(uv);
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y = -1; y <= 1; y++)
    {
        for(int x = -1; x <= 1; x++)
        {
            float2 lattice = float2(x, y);
            float2 offset = VoronoiRandom2(lattice + g, t);
            float d = distance(lattice + offset, f);
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
            }
        }
    }

    return res;
}

//x: distance field (Valley)
//yz: cell color
half3 Voronoi3(float2 uv, float t)
{
    int2 p = int2(floor(uv));
    float2 f = frac(uv);

    int2 mb;
    float2 mr;

    float3 res = float3(8.0, 0, 0);
    for( int j = -1; j <= 1; j++ )
    for( int i = -1; i <= 1; i++ )
    {
        int2 b = int2(i, j);
        float2 random = VoronoiRandom(p + b, t);
        //float2 r = float2(b) + random - f;
        float2 r = float2(b) + VoronoiRandom(p + b, t) - f;

        float d = dot(r, r);

        if(d < res.x)
        {
            res.x = d;
            res.yz = random;
            mr = r;
            mb = b;
        }
    }

    res.x = 8.0;
    for( int k = -2; k <= 2; k++ )
    for( int l = -2; l <= 2; l++ )
    {
        int2 b = mb + int2(l, k);
        float2  r = float2(b) + VoronoiRandom(p + b, t) - f;
        float d = dot(0.5 * (mr + r), normalize(r - mr));

        res.x = min(res.x, d);
    }

    return res;
}


#endif