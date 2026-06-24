# Repo Kastim Mod

R.E.P.O. için ekstra envanter slotları ve azaltılmış stamina tüketimi modu.

Güncel R.E.P.O. sürümleri (v0.4.x) ile uyumludur. Eski `MoreInventorySlots` modunun bozulan işlevleri bu modda yeniden yazıldı ve stamina ayarı eklendi.

## Özellikler

### Envanter Slotları
- 3 ile 10 arasında yapılandırılabilir slot sayısı (varsayılan: 5)
- Tam slot UI: ikonlar, numaralar ve pil göstergeleri
- 4-9 ve 0 tuşları ile ekstra slot kısayolları
- İsteğe bağlı numpad kısayolları
- Dolu slota item koyunca otomatik swap
- Host koruması (multiplayer)
- Round arası item takibi ve geri yükleme

### Stamina
- Koşu stamina tüketimini azaltır (varsayılan: %35 daha az)
- `BepInEx/config/kazhime.repokastimmod.cfg` dosyasından ayarlanabilir

## Kurulum

1. [BepInEx](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) kurulu olmalı
2. Mod manager (Gale / r2modman) ile yükleyin **veya**
3. `plugins/RepoKastimMod/RepoKastimMod.dll` dosyasını BepInEx plugins klasörüne kopyalayın

## Yapılandırma

İlk çalıştırmadan sonra `BepInEx/config/kazhime.repokastimmod.cfg` oluşur.

| Ayar | Varsayılan | Açıklama |
|------|------------|----------|
| Number Of Slots | 5 | Toplam envanter slotu (3-10) |
| Host Protection | true | Host, client slot limitini belirler |
| Keep Items In Truck | false | Round değişince itemlar kamyonda kalsın |
| Auto Swap Items | true | Dolu slotta swap yap |
| Extra Slot Hotkeys | true | 4-9, 0 tuşları |
| Numpad Hotkeys | true | Numpad tuşları |
| Sprint Drain Multiplier | 0.65 | 1.0 = vanilla, 0.65 = %35 daha az drain |

## Multiplayer Notları

- **Sadece client'ta mod var:** Her oyuncu kendi slot ayarını kullanır
- **Host'ta mod + Host Protection:** Client'lar host'un slot limitini aşamaz
- Stamina değişikliği client-side çalışır (her oyuncu kendi ayarını kullanır)

## Geliştirme

```bash
cd src
dotnet build -c Release
```

Çıktı: `release/RepoKastimMod.dll`

## Lisans

MIT
