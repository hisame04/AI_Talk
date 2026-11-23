# AI_Talk

初音ミクと会話するためのUnityアプリです。

## Local-Only Assets

以下のフォルダ内のファイルは、ライセンスの都合によりgitの追跡対象から外しています。

- `Assets/3Dmodel/`
- `Assets/Motion/`

フォルダの構造は以下の通りです。

```
Assets/
  3Dmodel/
    Materials/
    PLUSONE/
      Milfy/
    Tomarudo/
      HatsuneMikuNT/
    YYB Hatsune Miku_default/
    YYB Hatsune Miku_default_MotionMake/
    YYB Hatsune Miku_default_view/
    YYB Hatsune Miku_NT/
    yyb/
      source/
      textures/
  Motion/
    3期宮舞/
    べるぅがスタジオ自作モーション（４）/
    べるぅがスタジオ自作モーション（６）/
    べるぅがスタジオ自作モーション（７）/
```

## ローカル MikuTTS
ローカルでMikuTTSを立ち上げて使用することで、応答時間を短縮できます。
ローカル MikuTTSを使用するには以下のセットアップが必要です。

MikuTTS を Unity と同じ PC で起動し、Gradio API に接続します。MikuTTS はこの Unity リポジトリの外、`AI_Talk_prg/mikuTTS` に配置します。初回起動時には RVC モデル一式を Hugging Face から取得し、音声生成時にも `edge-tts` がインターネットを使います。十分な空き容量とネット接続が必要です。

前提は Python 3.10、Git、Git LFS です。以下のインストールは初回だけ行い、普段は起動コマンドだけ使います。既に `mikuTTS` フォルダがある場合は `git clone` を飛ばしてください。MikuTTS の `requirements.txt` は PyTorch 2.5.1 を指定しているため、2.6.0 への更新は最後に行います。後から `requirements.txt` を再インストールした場合は、再度 2.6.0 を入れてください。

### macOS

Python 3.10 と Git LFS をインストールしてからターミナルで実行します。Unity プロジェクトを別の場所に置いた場合は、最初の `cd` をその親フォルダに変更してください。

```bash
cd /Users/hisanagamegu/Desktop/Unity/AI_Talk_prg
git lfs install
git clone https://huggingface.co/spaces/John6666/mikuTTS mikuTTS
cd mikuTTS
python3.10 -m venv .venv
source .venv/bin/activate
python -m pip install -r pre-requirements.txt
python -m pip install -r requirements.txt
python -m pip install "gradio==6.26.0" spaces tensorboardX
python -m pip install --upgrade "torch==2.6.0" "torchaudio==2.6.0"
python -c "import torch, torchaudio; print(torch.__version__, torchaudio.__version__, torch.backends.mps.is_available())"
```

Apple GPU（MPS）で起動する場合：

```bash
TORCH_FORCE_NO_WEIGHTS_ONLY_LOAD=1 OMP_NUM_THREADS=1 GRADIO_SERVER_PORT=7860 python app.py
```

CPU で起動する場合：

```bash
TORCH_FORCE_NO_WEIGHTS_ONLY_LOAD=1 OMP_NUM_THREADS=1 GRADIO_SERVER_PORT=7860 \
python -c 'import torch; torch.backends.mps.is_available = lambda: False; import app'
```

`TORCH_FORCE_NO_WEIGHTS_ONLY_LOAD=1` は、PyTorch 2.6.0 で fairseq の HuBERT 読み込みを通すための設定です。チェックポイントを信頼できる場合にだけ使用してください。CPU 起動の Python コマンドは、その実行中だけ MPS の自動選択を無効にします。MikuTTS のソースは変更しません。

### Windows（PowerShell、未検証）

Python 3.10（`py -3.10` で起動できること）、Git と Git LFS をインストールします。Python パッケージのビルドに失敗する場合は Visual Studio Build Tools の C++ ツールと Windows SDK が必要になることがあります。Windows での `fairseq` のインストールと MikuTTS 本体の動作はまだ確認していません。取得した `app.py` 自体にも Windows では `spaces` 関連を削除する旨のコメントがあり、以下のコマンドだけで動作することは保証できません。

`C:\path\to\AI_Talk_prg` は Unity プロジェクトが入っている親フォルダの実際のパスに置き換えてください。PowerShell の実行ポリシーを変更せずに済むよう、仮想環境の Python を直接呼びます。

```powershell
cd 'C:\path\to\AI_Talk_prg'
git lfs install
git clone https://huggingface.co/spaces/John6666/mikuTTS mikuTTS
cd .\mikuTTS
py -3.10 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r pre-requirements.txt
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m pip install 'gradio==6.26.0' spaces tensorboardX
```

NVIDIA GPU で実行する場合は、GPU ドライバーと互換性のある CUDA 版の PyTorch 2.6.0 を入れます。以下は CUDA 12.6 版の例です。対応する CUDA 版は [PyTorch のインストール案内](https://docs.pytorch.org/get-started/previous-versions/)で確認してください。

```powershell
.\.venv\Scripts\python.exe -m pip install --upgrade torch==2.6.0 torchaudio==2.6.0 --index-url https://download.pytorch.org/whl/cu126
.\.venv\Scripts\python.exe -c "import torch, torchaudio; print(torch.__version__, torchaudio.__version__, torch.cuda.is_available())"
```

NVIDIA GPU を使わない場合は CPU 版を入れます。

```powershell
.\.venv\Scripts\python.exe -m pip install --upgrade torch==2.6.0 torchaudio==2.6.0 --index-url https://download.pytorch.org/whl/cpu
```

起動時は同じ PowerShell ウィンドウで環境変数を設定します。CPU 版なら CPU、CUDA 版で `torch.cuda.is_available()` が `True` なら GPU が自動選択されます。

```powershell
$env:TORCH_FORCE_NO_WEIGHTS_ONLY_LOAD = '1'
$env:OMP_NUM_THREADS = '1'
$env:GRADIO_SERVER_PORT = '7860'
.\.venv\Scripts\python.exe app.py
```