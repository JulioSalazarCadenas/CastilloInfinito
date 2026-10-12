using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Vacío del Castillo Infinito: cientos de estructuras flotando en cualquier
/// orientación, dibujadas con GPU instancing. No hay GameObjects ni colisiones,
/// por eso es casi gratis para la GPU/CPU y se puede subir mucho la cantidad.
/// Ponlo en un GameObject vacío en el origen de la escena.
/// </summary>
public class VacioCastillo : MonoBehaviour
{
    [Header("Qué se dibuja")]
    [Tooltip("Mallas variadas: salas, pagodas, escaleras. Si está vacío se usa un cubo.")]
    public Mesh[] mallas;

    [Tooltip("Material con 'Enable GPU Instancing' activado.")]
    public Material material;

    [Header("Distribución")]
    public int cantidad = 800;
    [Tooltip("Zona libre alrededor del centro, donde camina el jugador.")]
    public float radioMin = 40f;
    public float radioMax = 400f;
    [Tooltip("Tamaño máximo (radio) de una sola estructura, en metros.")]
    public float radioMaxEstructura = 250f;
    [Tooltip("Gira cada estructura solo en múltiplos de 90 grados, como en el anime. Apágalo para ángulos cualquiera.")]
    public bool alinearAEjes = true;
    public Vector2 escala = new Vector2(10f, 60f);
    public int semilla = 12345;

    // Los llena CastilloInfinito cuando cambia los cubos por casas y pagodas.
    [HideInInspector] public Material[] materialesSub;   // un material por submalla
    bool escalaUniforme;                                  // las casas no se deforman

    [Header("Movimiento")]
    [Tooltip("Giro lento de todo el vacío, en grados por segundo.")]
    public float velocidadGiro = 0.4f;

    [Header("Ambiente")]
    public Color colorNiebla = new Color(0.03f, 0.02f, 0.05f);
    [Range(0f, 0.05f)] public float densidadNiebla = 0.006f;

    const int MaxPorLote = 1023; // límite de DrawMeshInstanced

    List<Matrix4x4>[] locales;   // una lista por malla
    Matrix4x4[] buffer = new Matrix4x4[MaxPorLote];
    float angulo;

    void Awake()
    {
        if (mallas == null || mallas.Length == 0)
        {
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mallas = new[] { cubo.GetComponent<MeshFilter>().sharedMesh };
            Destroy(cubo);
        }

        if (material == null) material = CrearMaterialPorDefecto();

        ConfigurarAmbiente();
        Generar();
    }

    // Así no necesitas crear ningún material a mano para la primera prueba.
    Material CrearMaterialPorDefecto()
    {
        string nombre = GraphicsSettings.currentRenderPipeline != null
            ? "Universal Render Pipeline/Lit"
            : "Standard";

        Shader sh = Shader.Find(nombre);
        if (sh == null) sh = Shader.Find("Standard");

        var mat = new Material(sh) { enableInstancing = true };
        Color c = new Color(0.25f, 0.08f, 0.08f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        return mat;
    }

    void ConfigurarAmbiente()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = colorNiebla;
        RenderSettings.fogDensity = densidadNiebla;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = colorNiebla;
        }
    }

    /// <summary>Cambia el color y la densidad de la niebla (y el fondo de la cámara).</summary>
    public void AplicarAmbiente(Color color, float densidad)
    {
        colorNiebla = color;
        densidadNiebla = densidad;
        ConfigurarAmbiente();
    }

    /// <summary>Rehace todo el vacío con otra semilla (lo llama CastilloInfinito al cruzar una puerta).</summary>
    public void Regenerar(int nuevaSemilla)
    {
        semilla = nuevaSemilla;
        Generar();
    }

    /// <summary>Reemplaza los cubos por otras mallas (con un material por submalla) y rehace el vacío.</summary>
    public void UsarMallas(Mesh[] nuevas, Material[] materiales, Vector2 nuevaEscala)
    {
        mallas = nuevas;
        materialesSub = materiales;
        escala = nuevaEscala;
        escalaUniforme = true;
        Generar();
    }

    void Generar()
    {
        locales = new List<Matrix4x4>[mallas.Length];
        for (int m = 0; m < mallas.Length; m++) locales[m] = new List<Matrix4x4>();

        // Semilla propia para no alterar el Random global del juego.
        var estadoPrevio = Random.state;
        Random.InitState(semilla);

        for (int i = 0; i < cantidad; i++)
        {
            int idx = Random.Range(0, mallas.Length);
            // Las salas del castillo siempre están giradas en múltiplos de 90 grados (suelo, pared o techo
            // de otra sala), nunca torcidas; solo las escaleras y vigas dan las diagonales.
            Quaternion rot = alinearAEjes
                ? Quaternion.Euler(90f * Random.Range(0, 4), 90f * Random.Range(0, 4), 90f * Random.Range(0, 4))
                : Random.rotation;

            // Distancia del pivote de la malla a su punto más lejano. El pivote casi nunca
            // está en el centro (una escalera arranca en su base, una torre en el suelo),
            // así que hay que sumar también el desplazamiento del centro de la malla.
            Bounds caja = mallas[idx].bounds;
            float radioBase = Mathf.Max(caja.center.magnitude + caja.extents.magnitude, 0.01f);

            float s = Random.Range(escala.x, escala.y);
            if (escalaUniforme) s = Mathf.Min(s, radioMaxEstructura / radioBase);   // ninguna pieza gigante
            Vector3 esc = escalaUniforme
                ? Vector3.one * s
                : new Vector3(
                    s * Random.Range(0.6f, 1.4f),
                    s * Random.Range(0.6f, 1.4f),
                    s * Random.Range(0.6f, 1.4f));

            // Con ese radio, ningún borde puede invadir la zona libre del centro.
            float radio = radioBase * Mathf.Max(esc.x, esc.y, esc.z);
            float dMin = radioMin + radio;
            float dMax = Mathf.Max(radioMax, dMin + 1f);
            Vector3 pos = Random.onUnitSphere * Random.Range(dMin, dMax);

            locales[idx].Add(Matrix4x4.TRS(pos, rot, esc));
        }

        Random.state = estadoPrevio;
    }

    void Update()
    {
        if (material == null) return;

        angulo += velocidadGiro * Time.deltaTime;
        Matrix4x4 raiz = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, angulo, 0f), Vector3.one);

        for (int m = 0; m < mallas.Length; m++)
        {
            List<Matrix4x4> lista = locales[m];
            for (int i = 0; i < lista.Count; i += MaxPorLote)
            {
                int n = Mathf.Min(MaxPorLote, lista.Count - i);
                for (int j = 0; j < n; j++) buffer[j] = raiz * lista[i + j];

                // Una llamada por submalla (muro, madera, teja...), cada una con su material.
                for (int s = 0; s < mallas[m].subMeshCount; s++)
                {
                    Material mat = (materialesSub != null && s < materialesSub.Length) ? materialesSub[s] : material;
                    Graphics.DrawMeshInstanced(
                        mallas[m], s, mat, buffer, n,
                        null, ShadowCastingMode.Off, false);
                }
            }
        }
    }
}
