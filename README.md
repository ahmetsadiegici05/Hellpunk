# Hellpunk

Hellpunk, **Unity** ile geliştirilmiş, **2D platformer** türünde ve **gitar/ritim temalı** bir oyun projesidir.

Hellpunk, hem eğlenceli hem de zorlu bir oyun deneyimi sunuyor!

Oyunun temel fikri: Platformer oynanışı sırasında yetenek kullanımı, klasik “tek tuş” yerine **kısa süreli bir input sekansı** ile tetiklenir. Oyuncu, ekrana gelen yön tuşu dizisini **3 saniye içinde** doğru sırayla girerek ability’leri aktive eder.

## Oynanış Özeti

- Tür: 2D Platformer
- Tema: Gitar / ritim hissi
- Ana mekanik: Ability’ler için “input sequence” mini-challenge

### Input Sequence Mekaniği (Ability Kullanımı)
- Ability kullanmak istediğinde ekranda bir sekans belirir (ör. `↓ ↑ ←` gibi).
- Oyuncu bu sekansı **3 saniye içinde** doğru sırayla girmelidir.
- Örnek inputlar:
  - `↓` (aşağı ok)
  - `↑` (yukarı ok)
  - `←` (sol ok)

Başarılı olursa ability tetiklenir; yanlış sıra / süre aşımı olursa ability başarısız olur (veya cooldown/ceza gibi bir sonuç uygulanabilir).

## Proje Yapısı
Bu repo bir Unity projesidir ve tipik Unity klasörlerini içerir:
- `Assets/`
- `Packages/`
- `ProjectSettings/`

## Kurulum ve Çalıştırma

### Gereksinimler
- Unity Hub

### Projeyi Açma
1. Unity Hub’ı açın
2. **Add / Open** ile repo klasörünü seçin
3. Unity projeyi import edip açacaktır

### Oyunu Çalıştırma
- Unity Editor içinde sahneyi açıp **Play** tuşuna basın.

### Build Alma (İsteğe bağlı)
- `File > Build Settings` üzerinden platform seçip **Build** alabilirsiniz.

## Notlar
- Repo içinde bazı `.slnx` dosyaları bulunabilir. Bunlar IDE/çözüm tarafına yardımcıdır; oyunun ana çalışma ortamı Unity’dir.
