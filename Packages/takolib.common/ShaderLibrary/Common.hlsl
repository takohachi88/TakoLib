#ifndef TAKOLIB_BLEND_INCLUDED
#define TAKOLIB_BLEND_INCLUDED

#define VERTEX_COLOR_BLEND_MULTIPLY (_VertexColorBlend == 0)
#define VERTEX_COLOR_BLEND_ADDITIVE (_VertexColorBlend == 1)

#define VERTEX_COLOR_BLEND(baseColor, vertexColor) \
    baseColor.rgb = baseColor.rgb * vertexColor.rgb * VERTEX_COLOR_BLEND_MULTIPLY + (baseColor.rgb + vertexColor.rgb) * VERTEX_COLOR_BLEND_ADDITIVE;\
    baseColor.a = baseColor.a * vertexColor.a;\


#define MULTIPLY_RGB_A(input) input.rgb *= _MultiplyRgbA ? (input).a : 1

#endif