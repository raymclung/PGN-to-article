# PGN to Article

Membaca satu partai catur dari berkas PGN, lalu menuliskannya menjadi artikel.

```bash
pgn-to-article partai.pgn -o artikel.md
```

## Cara kerjanya

```
partai.pgn
    │
    ├─▶ PgnParser      urai header dan langkah
    ├─▶ EngineAnalysis  Stockfish menandai momen kritis   (opsional)
    ├─▶ PromptBuilder   susun konteks untuk model bahasa
    ├─▶ AiClient        Ollama / Gemini / Claude
    └─▶ Scrubber        rapikan keluaran
            │
            ▼
        artikel.md
```

`PromptBuilder` bagian yang paling menentukan. Ia tidak sekadar menempelkan notasi
partai ke dalam prompt, melainkan menyusun konteks: siapa pemainnya, pembukaan apa yang
dimainkan, di langkah mana posisi berubah, dan seberapa besar perubahannya menurut mesin.
Model bahasa lalu diminta menulis dengan bahan itu, bukan mengarang dari daftar langkah.

Keluarannya berupa JSON terstruktur — judul, isi artikel, prompt gambar, caption media
sosial, meta description, dan tag. Secara bawaan alat ini membongkarnya dan menulis
artikelnya saja; pakai `--json` kalau ingin seluruh amplopnya.

## Menjalankan

Butuh .NET 10.

```bash
dotnet build
dotnet run -- partai.pgn --no-engine -o artikel.md
```

Model bawaannya [Ollama](https://ollama.com) yang berjalan lokal, jadi tidak perlu kunci
API sama sekali:

```bash
export CHESSARTICLE_OLLAMAMODEL="qwen2.5:7b"
dotnet run -- partai.pgn --no-engine --lang id -o artikel.md
```

### Opsi

| Opsi | Arti |
|---|---|
| `-o`, `--out` | Berkas keluaran (bawaan: layar) |
| `--provider` | `ollama`, `gemini`, atau `claude` |
| `--lang` | `id` atau `en` |
| `--context` | Konteks tambahan: nama turnamen, babak, apa yang dipertaruhkan |
| `--words` | Panjang sasaran, misal `600-900` |
| `--no-engine` | Lewati analisis Stockfish |
| `--prompt-only` | Cetak prompt-nya saja, tanpa memanggil model |
| `--json` | Simpan amplop JSON utuh |
| `--html` | Render jadi HTML, bukan Markdown |

`--prompt-only` berguna saat menyetel gaya tulisan: kamu bisa melihat persis apa yang
dikirim ke model tanpa menunggu jawabannya.

## Konfigurasi

Semua nilai dibaca dari variabel lingkungan. Tidak ada satu pun rahasia yang perlu
disimpan dalam berkas di dalam repositori ini.

| Variabel | Untuk |
|---|---|
| `CHESSARTICLE_OLLAMAURL` | Alamat Ollama (bawaan `http://127.0.0.1:11434`) |
| `CHESSARTICLE_OLLAMAMODEL` | Nama model Ollama |
| `CHESSARTICLE_GEMINIAPIKEY` | Kunci Gemini |
| `CHESSARTICLE_CLAUDEAPIKEY` | Kunci Claude |
| `CHESSARTICLE_STOCKFISHPATH` | Lokasi biner Stockfish |
| `CHESSARTICLE_PYTHONPATH` | Perintah Python untuk pembantu analisis |
| `CHESSARTICLE_USAGELOG` | Berkas CSV pencatat pemakaian dan biaya model |

Kalau `CHESSARTICLE_USAGELOG` diisi, tiap panggilan model dicatat: jumlah token, durasi,
dan perkiraan biaya. Berguna untuk tahu berapa ongkos sebenarnya per artikel.

## Roster opsional

`PgnParser.RosterResolver` adalah titik sambung untuk memperkaya partai dengan data di
luar PGN — nama asli, gelar, klub, rating:

```csharp
PgnParser.RosterResolver = game =>
{
    game.WhiteFullName = "Jane Doe";
    game.WhiteTitle    = "IM";
    game.WhiteTeamName = "Acme Club";
};
```

Kalau tidak dipasang, semuanya bekerja dari header PGN apa adanya.

## Asal-usul

Alat ini diekstrak dari sistem penulisan artikel yang lebih besar. Yang dibawa ke sini
hanya bagian generiknya: PGN masuk, teks artikel keluar.

Yang sengaja **tidak** ikut: seluruh aplikasi web, panel admin, penyimpanan artikel,
penerbit blog, pencarian roster yang terikat basis data internal, serta segala hal yang
khusus milik penyelenggara turnamen tertentu.

Sepanjang pemindahan, kode juga diperbarui dari .NET Framework ke .NET 10 —
`JavaScriptSerializer` diganti `System.Text.Json`, `HttpUtility` diganti `WebUtility`,
`HttpContext.Server.MapPath` diganti path yang bisa dikonfigurasi, dan pembacaan rahasia
dipindah sepenuhnya ke variabel lingkungan.

## Belum dikerjakan

- Tiga pemanggilan HTTP masih memakai `WebRequest`, yang di .NET modern sudah usang.
  Perlu dipindahkan ke `HttpClient` — masih berfungsi, tapi memunculkan peringatan saat build
- Analisis mesin memanggil skrip Python di luar; idealnya berbicara langsung ke Stockfish lewat UCI
- Belum ada test otomatis

## Kontribusi

Dikembangkan atas penugasan dan arahan arsitek proyek tempat alat ini dipakai.

## Lisensi

[MIT](LICENSE)
