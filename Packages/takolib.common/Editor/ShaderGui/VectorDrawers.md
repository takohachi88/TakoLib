# ShaderLab Vector drawers

```shaderlab
Properties
{
    [Vector2] _Offset ("Offset", Vector) = (0, 0, 0, 0)
    [Vector3] _Direction ("Direction", Vector) = (0, 1, 0, 0)
    [MinMax(0, 10)] _Distance ("Distance Range", Vector) = (2, 8, 0, 0)
}
```

ShaderLabの宣言は `Vector` のまま、Inspectorの編集GUIを変更します。
`CustomEditor` の指定は不要です。独自ShaderGUIでは `MaterialEditor.ShaderProperty` を通して描画してください。
同じプロパティには上記属性のいずれか1つを指定します。

- `[Vector2]`: X・Yを編集します。
- `[Vector3]`: X・Y・Zを編集します。
- `[MinMax(lower, upper)]`: Xを最小値、Yを最大値として、数値欄と両端ハンドルのスライダーで編集します。引数なしの `[MinMax]` は0〜1です。

MinMaxの数値入力は許容範囲と反対側の値で制限され、X <= Yになります。
読み取りや再描画だけでは既存値を変更しません。デフォルト値も許容範囲内かつX <= Yで指定してください。
複数選択とUndoに対応します。Vector2/Vector3の編集は変更した成分だけ、MinMaxのスライダーはX・Yの組を適用します。
非表示の成分（Vector2/MinMaxのZ・W、Vector3のW）は各マテリアルで保持されます。

HLSLでは `float4` として宣言し、必要な成分を参照できます。

```hlsl
float4 _Offset;
float4 _Direction;
float4 _Distance;
// _Offset.xy / _Direction.xyz / _Distance.xy
```
