# Camera Shot Track / Volume Weight Track

## Camera Shot Track

1. TimelineにCamera Shot Trackを追加し、CameraをBindingに割り当てます。
2. Camera Shot Clipを追加し、位置の指定方法を選択します。
   - Spline: 単一の開いたSpline（Knotが2つ以上）を持つSplineContainerを指定します。
   - Direct: キーにワールド座標の位置・回転を記録します。
3. 追加するキーの項目を選び、再生ヘッドをClip内に置いてキードラッグ欄の右下の「+」を押します。
   同時刻にキーがある場合、そのキーに選択した項目を記録します。
4. 番号付きマーカーを選択すると、そのキーだけの詳細が下に表示されます。
   選択マーカーは黄色です。詳細の番号・Clip内時刻・Knot番号で編集対象を確認できます。
   「前のキー」「次のキー」でも移動できます。「-」は選択中のキーを削除します。
5. マーカーをドラッグして時刻を編集します。フレームにスナップし、Shiftで解除します。
   背景クリックはスクラブです。時刻の数値入力も可能です。
6. Splineの位置キーはKnot番号を指定します。Sceneの位置ハンドルはKnot、
   回転ハンドルは選択キーのカメラ回転を編集します。Knotの接線方向とカメラ回転は別です。

位置・回転・FoV・Lens Shift・手振れは項目ごとにキー間を補間します。
Lens Shift使用中はPhysical Cameraが有効になります。Clipの重なりで、
Spline / Directを含むカメラ状態をブレンドできます。

## Easing

共通APIはRuntime/Easing.csのTakoLib.Common.EasingとEasingTypeです。
Timelineへの依存はなく、Easing.Evaluate(EasingType.EaseInOutSine, progress)で使えます。
CustomはEasing.Evaluate(EasingType.Custom, curve, progress)を使用します。

[easings.net](https://easings.net/ja)に掲載されているSine / Quad / Cubic / Quart /
Quint / Expo / Circ / Back / Elastic / BounceのIn・Out・InOut（30種類）を選択できます。
Linear、Hold、Customも利用できます。全て共通Easingで評価します。

入力の進行度は0～1に制限します。出力は制限しないためBack / ElasticやCustomで
行き過ぎを表現できます。Direct位置・回転・Lens Shiftはその値を使用します。
Splineはパス全体の始終端、FoVは1～179、Volume Weightは0～1で制限します。
Easingは出発側のキーに設定し、そのキーが持つ各項目の次のキーまでに適用します。

## 手振れ

- Seed: 同じキー設定・時刻で同じ揺れを再現します。
- 位置の揺れ幅 / 回転の揺れ幅: カメラのローカル軸ごとの振幅（m / 度）。
- Scale: 両方の振幅に掛ける倍率。0で停止、1で設定どおり、2で2倍。
- Frequency: 新規キーの初期値は8。ノイズを進める速さであり、周期的な往復回数ではありません。
  既存のキーの値は変更しません。
- Phase Offset: 同じSeedのノイズの開始位置をずらします。通常は0で構いません。
  秒単位ではなくノイズの位相です。異なるFrequencyを持つClip同士の連続性を自動保証するものではありません。
- 一時無効化: 設定を残して揺れだけを停止します。

Frequencyを時間積分して位相を求めるため、順不同のスクラブでも結果は同じです。
拡張EasingとCustomの積分は固定128分割の数値近似です。Frequencyの負値は0に制限します。
積分結果は区間ごとにキャッシュし、キーの時刻・Frequency・Easing・カーブの変更時に再計算します。

## Volume Weight Track

Trackを追加し、VolumeをBindingに割り当て、Volume Weight Clip内の時刻・Weight・Easingを指定します。
同じVolumeを制御するClipの重なりはWeightをブレンドします。Volume Profileは変更しません。
こちらのWeightはUnity Volume本来の0～1の重みであり、手振れのScaleとは異なります。

## 実行時の負荷

- Playable生成時にカメラ評価用のキャッシュを準備します。
- Knotデータは添字アクセスし、イテレーターによるGC Allocを避けます。
- Splineの距離変換は形状・Transform・Undo等の変更時に更新します。
- 手振れは区間の累積積分と二分探索を使い、毎フレームの全区間積分を省きます。
- キャッシュ準備後のDirect・Spline・Customカーブ・Volume評価、およびカメラとVolumeを含む
  Timelineの1,000回評価でGC.Allocマーカーが0回になることを自動検証しています。
  計測器が確保を検知できることも別のテストで確認しています。
- 初期化、グラフ再構築、キー数やカーブ点数の増加によるキャッシュ拡張、Editorの描画は
  GC Allocゼロの対象外です。Playerビルド全体のフレーム負荷を保証するものではありません。

Unity 6000.7.0b1のEditorで、32キー・全項目・EaseInOutElasticの同じ条件を1,000回評価した参考値:

| 位置モード | 変更前 | 変更後 | GC.Alloc回数（変更前 → 変更後） |
| --- | ---: | ---: | ---: |
| Direct | 135.94ms | 3.23ms | 0 → 0 |
| Spline | 125.28ms | 8.58ms | 34,000 → 0 |

ウォームアップ後の同期評価のみを測定しています。Splineのキーは同一Knotを参照する計測用設定です。
時間はマシン・キー数・パス構造・カーブに依存します。

## 動作と制限

- Clip内の秒を使用し、Clip InとTime Scaleに従います。端のキーの外側は端の値を保持します。
- 同じKnotへの複数キーで停止・回転できます。Knot IDは通常の挿入・削除に追従します。
  対応Knotが削除されると再割り当てまでClipを評価しません。
- Clip間の空白、Director停止、Timelineプレビュー終了時には元の状態へ復元します。
- 同じCameraを別のCamera Track・Animation Track・Cinemachineから同時に制御しないでください。
- Splineの閉路・複数Spline・Knot並べ替え・Spline反転/結合/分割・Sub-Timelineは未対応です。
- 回転補間はQuaternionの最短経路です。複数回転は未対応です。
- Timeline本体のClip内の縦線は時刻の目印です。キー選択・ドラッグはInspector内で行います。
- 使用感と実際の操作確認は利用者が担当します。コンパイルと数値・状態復元の自動検証は実装側で行います。
