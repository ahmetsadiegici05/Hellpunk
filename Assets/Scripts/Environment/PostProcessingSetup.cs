using UnityEngine;

/// <summary>
/// Post-Processing kurulum rehberi
/// </summary>
public class PostProcessingSetup : MonoBehaviour
{
    [Header("POST-PROCESSING KURULUM REHBERİ")]
    [TextArea(15, 20)]
    public string instructions = @"
=== POST-PROCESSING KURULUMU ===

1. Bu objeye 'Volume' component ekle
   (Add Component -> Volume ara)

2. Volume ayarları:
   - Mode: Global
   - Profile: New tıkla

3. Profile'a efekt ekle:
   - Add Override -> Vignette
   - Add Override -> Bloom
   - Add Override -> Color Adjustments

4. Main Camera ayarı:
   - Main Camera seç
   - Rendering -> Post Processing: ✓

=== ÖNERİLEN DEĞERLER ===

VIGNETTE:
- Intensity: 0.4
- Smoothness: 0.5

BLOOM:
- Threshold: 0.8
- Intensity: 0.6

COLOR ADJUSTMENTS:
- Contrast: 15
- Saturation: -5
";

    [Header("Hızlı Referans Değerler")]
    public float vignetteIntensity = 0.4f;
    public float bloomIntensity = 0.6f;
    public float contrast = 15f;
}
