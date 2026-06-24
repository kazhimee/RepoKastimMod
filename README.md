# Repo Kastim Mod

R.E.P.O. için ekstra envanter slotları modu. Güncel R.E.P.O. sürümleri (v0.4.x) ile uyumludur.

## Özellikler

- 3 ile 10 arasında yapılandırılabilir slot sayısı (varsayılan: 5)
- Tam slot UI: ikonlar, numaralar ve pil göstergeleri
- 4-9 ve 0 tuşları ile ekstra slot kısayolları
- İsteğe bağlı numpad kısayolları
- Dolu slota item koyunca otomatik swap
- Host koruması (multiplayer)
- Round arası item takibi ve geri yükleme
- Singleplayer ve multiplayer desteği

## Kurulum

1. [BepInEx](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/), [MenuLib](https://thunderstore.io/c/repo/p/nickklmao/MenuLib/) ve [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) kurulu olmalı (mod manager kullanırsan otomatik kurar)
2. Zip'i açıp `RepoKastımMod` klasörünü `BepInEx/plugins/` altına kopyalayın
3. Oyun içinde ana menüde **Mods** butonu → **Repo Kastim Mod** sekmesinden ayarlar değiştirilebilir (oyun içi, restart gerektirmez)

## Yapılandırma

`BepInEx/config/kazhime.repokastimmod.cfg`

| Ayar | Varsayılan | Açıklama |
|------|------------|----------|
| Number Of Slots | 5 | Toplam envanter slotu (3-10) |
| Host Protection | true | Host, client slot limitini belirler |
| Keep Items In Truck | false | Round değişince itemlar kamyonda kalsın |
| Auto Swap Items | true | Dolu slotta swap yap |
| Extra Slot Hotkeys | true | 4-9, 0 tuşları |
| Numpad Hotkeys | true | Numpad tuşları |
| Alignment | Center | Slot satırının ekrandaki yatay konumu: `Left`, `Center`, `Right` (kanvas genişliğine göre otomatik) |
| Alignment Strength | 0.85 | Left/Right modunda ekran kenarına ne kadar yaklaşılsın (0–1) |
| Alignment Fine Offset | 0 | Manuel yatay ince ayar (-200 ile +200) |
| Vertical Offset | 0 | Manuel dikey ince ayar (-200 ile +200) |

## Multiplayer

- **Tüm oyuncularda mod olmalı** — ekstra slotlar her client'ta ayrı kurulur
- **Host Protection açıksa:** host'un slot sayısı client'lar için üst limit olur
- Host'ta mod yoksa client'lar ekstra slotları kullanabilir; host koruması devreye girmez
- **Vanilla Steam Lobby + Photon matchmaking aynen çalışır** — mod hiçbir network kodu eklemez

## Geliştirme

```bash
cd src
dotnet build -c Release
```

## Lisans

MIT
