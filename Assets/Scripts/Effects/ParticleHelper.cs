using UnityEngine;

/// <summary>
/// Parçacık efektleri için yardımcı sınıf
/// Tüm efekt sistemleri bu helper'ı kullanarak tutarlı ve estetik parçacıklar oluşturur
/// </summary>
public static class ParticleHelper
{
    private static Texture2D softCircleTexture;
    private static Texture2D glowTexture;
    private static Texture2D sparkTexture;
    
    /// <summary>
    /// Yumuşak kenarlı yuvarlak texture
    /// </summary>
    public static Texture2D GetSoftCircleTexture()
    {
        if (softCircleTexture != null) return softCircleTexture;
        
        int size = 64;
        softCircleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        softCircleTexture.filterMode = FilterMode.Bilinear;
        softCircleTexture.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = dist / radius;
                
                // Gaussian-like falloff for smooth edges
                float alpha = Mathf.Exp(-normalizedDist * normalizedDist * 3f);
                alpha = Mathf.Clamp01(alpha);
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        softCircleTexture.SetPixels(pixels);
        softCircleTexture.Apply();
        
        return softCircleTexture;
    }
    
    /// <summary>
    /// Parlama efekti için texture
    /// </summary>
    public static Texture2D GetGlowTexture()
    {
        if (glowTexture != null) return glowTexture;
        
        int size = 64;
        glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        glowTexture.filterMode = FilterMode.Bilinear;
        glowTexture.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = dist / radius;
                
                // Çok yumuşak glow
                float alpha = 1f - Mathf.Pow(normalizedDist, 0.5f);
                alpha = Mathf.Clamp01(alpha) * 0.8f;
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        glowTexture.SetPixels(pixels);
        glowTexture.Apply();
        
        return glowTexture;
    }
    
    /// <summary>
    /// Kıvılcım/spark efekti için texture
    /// </summary>
    public static Texture2D GetSparkTexture()
    {
        if (sparkTexture != null) return sparkTexture;
        
        int size = 32;
        sparkTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        sparkTexture.filterMode = FilterMode.Bilinear;
        sparkTexture.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = dist / radius;
                
                // Merkez çok parlak, kenarlar hızlı soluyor
                float alpha = Mathf.Pow(1f - Mathf.Clamp01(normalizedDist), 3f);
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        sparkTexture.SetPixels(pixels);
        sparkTexture.Apply();
        
        return sparkTexture;
    }
    
    /// <summary>
    /// Additive (ışık ekleme) material oluştur - ışık efektleri için
    /// </summary>
    public static Material CreateAdditiveMaterial(Texture2D texture = null)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
        {
            // Fallback shader
            mat = new Material(Shader.Find("Unlit/Transparent"));
        }
        
        mat.SetTexture("_MainTex", texture ?? GetSoftCircleTexture());
        
        // Additive blending
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        
        return mat;
    }
    
    /// <summary>
    /// Alpha blend material oluştur - toz/duman efektleri için
    /// </summary>
    public static Material CreateAlphaBlendMaterial(Texture2D texture = null)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
        {
            mat = new Material(Shader.Find("Unlit/Transparent"));
        }
        
        mat.SetTexture("_MainTex", texture ?? GetSoftCircleTexture());
        
        // Alpha blending
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        
        return mat;
    }
    
    /// <summary>
    /// ParticleSystemRenderer'a material uygula
    /// </summary>
    public static void ApplyMaterial(ParticleSystemRenderer renderer, bool additive = true, Texture2D customTexture = null)
    {
        if (renderer == null) return;
        
        Material mat = additive 
            ? CreateAdditiveMaterial(customTexture) 
            : CreateAlphaBlendMaterial(customTexture);
        
        renderer.material = mat;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 100;
    }
}
