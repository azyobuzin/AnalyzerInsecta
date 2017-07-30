# AnalyzerInsecta
![出力サンプル](http://cdn-ak.f.st-hatena.com/images/fotolife/a/azyobuzin/20170627/20170627234431.png)

Roslyn アナライザーのためのデバッグツールです。

- プロジェクトとアナライザーアセンブリを指定してデバッグ実行すると、デバッガーでアナライザーをデバッグすることができます。
- Visual Studio を起動しなくても、どこに警告が表示されるのかひと目でわかります。

# 使い方
## コマンドラインから実行
```
.\AnalyzerInsecta.exe --projects foo.csproj --analyzers analyzer.dll
```

カレントディレクトリに [AnalyzerInsecta.json](https://github.com/azyobuzin/AnalyzerInsecta/blob/7f3086bf6d9ee8e495bc4e2871ce2dd6486ab671/TestProjects/SampleAnalyzers/AnalyzerInsecta.json) を配置することで、その設定ファイルを自動的に読み込みます。
AnalyzerInsecta.json の書き方は、 [Config.cs](https://github.com/azyobuzin/AnalyzerInsecta/blob/7f3086bf6d9ee8e495bc4e2871ce2dd6486ab671/AnalyzerInsecta/Config.cs#L12-L17) を参考にしてください。

## Visual Studio でデバッグ実行（PCL プロジェクト）
[SampleAnalyzers.csproj](https://github.com/azyobuzin/AnalyzerInsecta/blob/7f3086bf6d9ee8e495bc4e2871ce2dd6486ab671/TestProjects/SampleAnalyzers/SampleAnalyzers.csproj#L54-L59) のようにデバッグ時に起動するプログラムとして設定します。

## Visual Studio でデバッグ実行（.NET Standard プロジェクト）
.NET Framework 用のデバッガーを起動させるために、 `TargetFramework` を `net46` に設定してから、デバッグ実行の設定（launchSettings.json）で AnalyzerInsecta を実行するように設定します。

例えば、ソリューション構成を追加して、 [SampleNetStandardAnalyzers.csproj](https://github.com/azyobuzin/AnalyzerInsecta/blob/7f3086bf6d9ee8e495bc4e2871ce2dd6486ab671/TestProjects/SampleNetStandardAnalyzers/SampleNetStandardAnalyzers.csproj#L7-L9) のように `TargetFramework` を分岐させれば、デバッグ時だけ `net46` にして、ビルド時には `netstandard` を使うことができます。

# 協力者募集中
## 出力 HTML のデザイン
現状かなりやっつけになっています。新しいデザインの提案や実際にコーディングしてくれる方を募集しています。

現在のコードは Dart で書かれていますが、 DockSpawn を使いたかっただけなので、 Dart である必要はありません。ただし、出力を 1 HTML ファイルにまとめることができることが望ましいです。

（シンタックスハイライトはそのうちやります。ユルシテ）

## 使ってみての要望
ぜひ、お願いします。

## 英語化
日本語ですら不自由なので、英語なんて書けません。タスケテ
