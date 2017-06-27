# AnalyzerInsecta
![出力サンプル](http://cdn-ak.f.st-hatena.com/images/fotolife/a/azyobuzin/20170627/20170627234431.png)

Roslyn アナライザーのためのデバッグツールです。

- プロジェクトとアナライザーアセンブリを指定してデバッグ実行すると、デバッガーでアナライザーをデバッグすることができます。
- Visual Studio を起動しなくても、どこに警告が表示されるのかひと目でわかります。

# 使い方
```
.\AnalyzerInsecta.exe --projects foo.csproj --analyzers analyzer.dll
```
