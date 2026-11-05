# Texture Generator integration

生成機能は独立パッケージ com.texturetools.generator に分離しています。
本パッケージはバージョン0.1.0を必須依存として宣言します。

## 利用プロジェクトへの導入

生成ツールのGit URL:
https://github.com/takohachi88/TextureTools.git

パッケージはリポジトリ直下にあるため ?path= は不要です。
Git配布では、Package Managerで生成ツールと本パッケージをそれぞれ追加してください。
パッケージのpackage.jsonには他パッケージのGit URLを書けないため、Git配布だけでの自動取得には対応していません。

## このUnityプロジェクトでの開発

Packages/com.texturetools.generator は上記リポジトリのサブモジュールです。
生成ツールの変更はこのフォルダーでコミット・Pushし、親プロジェクトで参照コミットの更新をコミットします。
初回の取得は git clone --recurse-submodules、既存のチェックアウトでの取得は git submodule update --init --recursive を使用してください。
開発時はサブモジュール内の作業ブランチに切り替えて編集してください。

操作方法は独立パッケージのREADME.mdを参照してください。