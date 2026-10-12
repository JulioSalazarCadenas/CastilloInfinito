using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Castillo Infinito jugable en primera persona.
/// Construye por código: el jugador, una sala con puertas shoji y, cada vez que
/// cruzas una puerta, destruye la sala y genera otra distinta (tamaño, colores,
/// columnas, luces, techo abierto o cerrado) mientras el vacío cambia de semilla.
/// En realidad nunca te mueves del origen: es un teletransporte escondido tras
/// un fundido muy corto.
/// Ponlo en el mismo objeto que VacioCastillo (o en cualquiera de la escena).
/// </summary>
public class CastilloInfinito : MonoBehaviour
{
    [Header("Jugador")]
    public float velocidad = 4f;
    public float velocidadCorrer = 8f;
    public float salto = 6f;
    public float gravedad = -20f;
    public float sensibilidad = 2f;

    [Header("Salas (se sortean cada vez)")]
    public Vector2 ancho = new Vector2(10f, 22f);
    public Vector2 largo = new Vector2(12f, 34f);
    public Vector2 alto = new Vector2(4.5f, 9f);
    [Range(0f, 1f)] public float probabilidadTechoAbierto = 0.5f;
    public int columnasMax = 4;

    [Header("Balcones exteriores (al aire libre, sobre el vacío)")]
    [Range(0f, 1f)] public float probabilidadExterior = 0.4f;
    public Vector2 anchoExterior = new Vector2(14f, 22f);
    public Vector2 largoExterior = new Vector2(16f, 28f);
    [Tooltip("Altura del parapeto. Si la subes mucho, ya no se ve el vacío.")]
    public float alturaParapeto = 1.4f;

    [Header("Vacío (estructuras que flotan alrededor)")]
    public int cantidadEstructuras = 320;
    [Tooltip("Radio alrededor del centro que se deja libre. Las salas miden menos de 25 m.")]
    public float radioLibre = 30f;
    public Vector2 escalaEstructuras = new Vector2(2f, 9f);
    [Tooltip("Giro lento de todo el vacío, en grados por segundo. En 0 las estructuras quedan alineadas con las salas.")]
    public float giroVacio = 0f;

    [Header("Ambiente (neblina ámbar y luz cálida)")]
    public Color colorNiebla = new Color(0.14f, 0.075f, 0.03f);
    [Range(0f, 0.02f)] public float densidadNiebla = 0.0035f;
    public Color colorAmbiente = new Color(0.20f, 0.12f, 0.07f);
    [Range(0f, 2f)] public float intensidadSol = 0.5f;

    [Header("Puertas")]
    public float anchoPuerta = 2.2f;
    public float altoPuerta = 3f;
    [Tooltip("A qué distancia de la puerta se activa el cruce.")]
    public float distanciaCruce = 0.9f;
    public float fundidoSalida = 0.15f;
    public float fundidoEntrada = 0.35f;

    const float Espesor = 0.4f;

    struct Puerta
    {
        public Vector3 pos;     // centro de la puerta, a nivel del piso
        public Vector3 dentro;  // normal que apunta hacia el interior de la sala
        public Vector3 lado;    // dirección a lo largo de la pared
    }

    GameObject jugador, sala;
    Transform cabeza;
    CharacterController cc;
    VacioCastillo vacio;
    Material matPiso, matPared, matColumna, matPuerta, matTatami;
    Material[] matsDecor;      // un material por submalla de ConstructorMalla
    Texture2D texTatami;
    readonly List<Puerta> puertas = new List<Puerta>();
    Puerta entrada;
    float yaw, pitch, velY, alfa;
    bool transicion;

    // ------------------------------------------------------------------ inicio

    void Start()
    {
        vacio = FindFirstObjectByType<VacioCastillo>();
        CrearMateriales();
        ConfigurarLuz();

        // El vacío pasa de cubos a torres de galerías, escaleras, vigas, pagodas y casas.
        if (vacio != null)
        {
            vacio.cantidad = cantidadEstructuras;
            vacio.radioMin = radioLibre;
            vacio.velocidadGiro = giroVacio;
            vacio.AplicarAmbiente(colorNiebla, densidadNiebla);
            vacio.UsarMallas(ArquitecturaJaponesa.MallasDelVacio(), matsDecor, escalaEstructuras);
        }

        CrearJugador();
        ConstruirSala();
        ColocarJugador(entrada, 0f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void CrearMateriales()
    {
        matPiso = NuevoMaterial();
        matPared = NuevoMaterial();
        matColumna = NuevoMaterial();
        matPuerta = NuevoMaterial();
        matTatami = NuevoMaterial();

        Pintar(matPiso, new Color(0.12f, 0.08f, 0.05f));
        texTatami = DecorJapones.TexturaTatami();
        Pintar(matTatami, new Color(0.85f, 0.85f, 0.8f));
        matTatami.mainTexture = texTatami;

        // Materiales de las estructuras (vacío y decoración).
        matsDecor = new Material[ConstructorMalla.NumSub];
        matsDecor[ConstructorMalla.Muro] = Hecho(new Color(0.42f, 0.34f, 0.20f), 0f);    // papel apagado y tatami
        matsDecor[ConstructorMalla.Madera] = Hecho(new Color(0.16f, 0.09f, 0.05f), 0f);
        matsDecor[ConstructorMalla.Teja] = Hecho(new Color(0.06f, 0.05f, 0.05f), 0f);    // laca negra
        matsDecor[ConstructorMalla.Laca] = Hecho(new Color(0.55f, 0.06f, 0.04f), 0f);
        matsDecor[ConstructorMalla.Piedra] = Hecho(new Color(0.28f, 0.28f, 0.30f), 0f);
        matsDecor[ConstructorMalla.Papel] = Hecho(new Color(0.95f, 0.82f, 0.55f), 1.2f);
    }

    Material Hecho(Color c, float emision)
    {
        Material mat = NuevoMaterial();
        mat.enableInstancing = true;
        Pintar(mat, c, emision);
        return mat;
    }

    Material NuevoMaterial()
    {
        string nombre = GraphicsSettings.currentRenderPipeline != null
            ? "Universal Render Pipeline/Lit"
            : "Standard";
        Shader sh = Shader.Find(nombre);
        if (sh == null) sh = Shader.Find("Standard");
        return new Material(sh);
    }

    void Pintar(Material m, Color c, float emision = 0f)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (emision > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emision);
        }
    }

    // Luz ambiente plana y sol tenue y cálido: lo que ilumina de verdad son los faroles y el papel.
    void ConfigurarLuz()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = colorAmbiente;

        foreach (Light luz in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (luz.type != LightType.Directional) continue;
            luz.color = new Color(1f, 0.72f, 0.45f);
            luz.intensity = intensidadSol;
        }
    }

    void CrearJugador()
    {
        jugador = new GameObject("Jugador");
        cc = jugador.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        var h = new GameObject("Cabeza");
        cabeza = h.transform;
        cabeza.SetParent(jugador.transform, false);
        cabeza.localPosition = new Vector3(0f, 1.6f, 0f);

        // Reutiliza la cámara de la escena; si no hay, crea una.
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Camara");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.transform.SetParent(cabeza, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
        cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = colorNiebla;
    }

    // ------------------------------------------------------------ construcción

    GameObject Caja(string nombre, Vector3 centro, Vector3 tam, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(sala.transform, false);
        go.transform.position = centro;
        go.transform.localScale = tam;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // Una pieza de pared entre dos posiciones (a lo largo de la pared) y dos alturas.
    void Tramo(Vector3 centro, Vector3 lado, bool enX, float desde, float hasta,
               float yMin, float yMax, Material mat)
    {
        float len = hasta - desde;
        float hh = yMax - yMin;
        if (len < 0.01f || hh < 0.01f) return;

        float medio = (desde + hasta) / 2f;
        Vector3 c = centro + lado * medio + Vector3.up * (yMin + hh / 2f);
        Vector3 tam = enX ? new Vector3(len, hh, Espesor) : new Vector3(Espesor, hh, len);
        Caja("Pared", c, tam, mat);
    }

    void ConstruirSala()
    {
        sala = new GameObject("Sala");
        puertas.Clear();

        // Parte de las salas son balcones exteriores abiertos sobre el vacío; el resto, interiores con tatami.
        bool exterior = Random.value < probabilidadExterior;
        float w, l, h;
        bool abierto;
        if (exterior)
        {
            w = Random.Range(anchoExterior.x, anchoExterior.y);
            l = Random.Range(largoExterior.x, largoExterior.y);
            h = alturaParapeto;
            abierto = true;
        }
        else
        {
            w = Random.Range(ancho.x, ancho.y);
            l = Random.Range(largo.x, largo.y);
            h = Random.Range(alto.x, alto.y);
            abierto = Random.value < probabilidadTechoAbierto;
        }

        if (exterior)
        {
            Pintar(matPared, new Color(0.07f, 0.04f, 0.025f));       // parapeto de madera oscura
            Pintar(matPiso, new Color(0.22f, 0.13f, 0.075f));        // tablas del piso
        }
        else
        {
            Pintar(matPared, Color.HSVToRGB(Random.Range(0f, 0.07f), 0.55f, 0.28f));
            Pintar(matPiso, new Color(0.12f, 0.08f, 0.05f));
        }
        Pintar(matColumna, Color.HSVToRGB(Random.Range(0f, 0.07f), 0.5f, 0.18f));
        Pintar(matPuerta, new Color(0.92f, 0.86f, 0.7f), 0.5f);

        Caja("Piso", new Vector3(0f, -0.25f, 0f),
             new Vector3(w + 2f * Espesor, 0.5f, l + 2f * Espesor), matPiso);
        if (!exterior) PonerTatami(w, l);
        if (!abierto)
            Caja("Techo", new Vector3(0f, h + Espesor / 2f, 0f),
                 new Vector3(w + 2f * Espesor, Espesor, l + 2f * Espesor), matColumna);

        // Reparte entre 2 y 4 puertas por las cuatro paredes sin que se encimen.
        var offs = new List<float>[4];
        for (int i = 0; i < 4; i++) offs[i] = new List<float>();
        int objetivo = Random.Range(2, 5);
        int total = 0;
        for (int intento = 0; intento < 60 && total < objetivo; intento++)
        {
            int p = Random.Range(0, 4);
            float dim = p < 2 ? w : l;
            float lim = dim / 2f - anchoPuerta / 2f - 1.2f;
            float d = Random.Range(-lim, lim);
            bool choca = false;
            foreach (float o in offs[p])
                if (Mathf.Abs(o - d) < anchoPuerta + 1.5f) choca = true;
            if (choca) continue;
            offs[p].Add(d);
            total++;
        }
        if (total == 0) offs[0].Add(0f);

        for (int p = 0; p < 4; p++) ConstruirPared(p, w, l, h, offs[p]);

        entrada = puertas[Random.Range(0, puertas.Count)];

        // Decoración japonesa sin colisión (postes, shoji, vigas, faroles / parapeto, pórticos y faroles).
        var decor = new ConstructorMalla();
        List<Vector3> luces;
        if (exterior)
            luces = DecorJapones.DecorarBalcon(decor, w, l, h, offs, anchoPuerta, Espesor);
        else
            luces = DecorJapones.DecorarSala(decor, w, l, h, !abierto, offs, anchoPuerta, Espesor);
        PonerDecor(decor);

        if (!exterior) ColocarColumnas(w, l, h);

        CrearLuces(luces, exterior);
    }

    void PonerTatami(float w, float l)
    {
        // La textura es una estera de 1,8 x 0,9 m: se repite para cubrir toda la sala.
        matTatami.mainTextureScale = new Vector2((w + Espesor) / 1.8f, (l + Espesor) / 0.9f);

        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Tatami";
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(sala.transform, false);
        q.transform.position = new Vector3(0f, 0.01f, 0f);
        q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        q.transform.localScale = new Vector3(w + Espesor, l + Espesor, 1f);
        q.GetComponent<MeshRenderer>().sharedMaterial = matTatami;
    }

    void PonerDecor(ConstructorMalla decor)
    {
        var go = new GameObject("Decor");
        go.transform.SetParent(sala.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = decor.Construir("DecorSala");
        go.AddComponent<MeshRenderer>().sharedMaterials = matsDecor;
    }

    void ColocarColumnas(float w, float l, float h)
    {
        int nCol = Random.Range(0, columnasMax + 1);
        for (int i = 0; i < nCol; i++)
        {
            for (int intento = 0; intento < 12; intento++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-w / 2f + 2f, w / 2f - 2f), 0f,
                    Random.Range(-l / 2f + 2f, l / 2f - 2f));
                if (CercaDePuerta(pos, 3.5f)) continue;
                Caja("Columna", new Vector3(pos.x, h / 2f, pos.z), new Vector3(0.8f, h, 0.8f), matColumna);
                break;
            }
        }
    }

    void CrearLuces(List<Vector3> posiciones, bool exterior)
    {
        Color colorLuz = Color.HSVToRGB(Random.Range(0.04f, 0.10f), 0.6f, 1f);
        int maxLuces = exterior ? 8 : 5;
        int paso = Mathf.Max(1, Mathf.CeilToInt(posiciones.Count / (float)maxLuces));
        for (int i = 0; i < posiciones.Count; i += paso)
        {
            var g = new GameObject("Luz");
            g.transform.SetParent(sala.transform, false);
            g.transform.position = posiciones[i];
            var luz = g.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = colorLuz;
            luz.intensity = exterior ? 4f : 5f;
            luz.range = exterior ? 14f : 10f;
        }
    }

    // Las mallas de decoración son propias de cada sala: se liberan al destruirla.
    void LimpiarMallas()
    {
        foreach (MeshFilter mf in sala.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh != null && mf.sharedMesh.name == "DecorSala") Destroy(mf.sharedMesh);
        }
    }

    bool CercaDePuerta(Vector3 pos, float radio)
    {
        foreach (var d in puertas)
        {
            Vector3 f = d.pos + d.dentro * 2f;
            f.y = 0f;
            if ((pos - f).sqrMagnitude < radio * radio) return true;
        }
        return false;
    }

    void ConstruirPared(int idx, float w, float l, float h, List<float> offsets)
    {
        Vector3 dentro, lado;
        bool enX;
        switch (idx)
        {
            case 0:  dentro = new Vector3(0f, 0f, -1f); lado = Vector3.right;   enX = true;  break;
            case 1:  dentro = new Vector3(0f, 0f, 1f);  lado = Vector3.right;   enX = true;  break;
            case 2:  dentro = new Vector3(-1f, 0f, 0f); lado = Vector3.forward; enX = false; break;
            default: dentro = new Vector3(1f, 0f, 0f);  lado = Vector3.forward; enX = false; break;
        }

        float semi = enX ? l / 2f : w / 2f;               // distancia del centro a esta pared
        float largoPared = (enX ? w : l) + Espesor;       // un poco más largo para cerrar esquinas
        Vector3 centro = -dentro * semi;

        offsets.Sort();
        float cursor = -largoPared / 2f;
        foreach (float d in offsets)
        {
            float a = d - anchoPuerta / 2f;
            float b = d + anchoPuerta / 2f;

            Tramo(centro, lado, enX, cursor, a, 0f, h, matPared);          // pared hasta la puerta
            Tramo(centro, lado, enX, a, b, altoPuerta, h, matPared);        // dintel sobre la puerta
            cursor = b;

            Vector3 pos = centro + lado * d;
            Vector3 tam = enX ? new Vector3(anchoPuerta, altoPuerta, 0.08f)
                              : new Vector3(0.08f, altoPuerta, anchoPuerta);
            Caja("PuertaShoji", pos + Vector3.up * (altoPuerta / 2f), tam, matPuerta);

            puertas.Add(new Puerta { pos = pos, dentro = dentro, lado = lado });
        }
        Tramo(centro, lado, enX, cursor, largoPared / 2f, 0f, h, matPared);
    }

    void ColocarJugador(Puerta e, float yawRelativo)
    {
        cc.enabled = false;   // si no, el CharacterController ignora el cambio de posición
        jugador.transform.position = e.pos + e.dentro * 1.2f + Vector3.up * 0.05f;
        cc.enabled = true;

        yaw = Mathf.Atan2(e.dentro.x, e.dentro.z) * Mathf.Rad2Deg + yawRelativo;
        jugador.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        velY = 0f;
    }

    // ------------------------------------------------------------------ juego

    void Update()
    {
        if (transicion || jugador == null) return;

        Mirar();
        Mover();
        RevisarPuertas();
    }

    void Mirar()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (LeerClick())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        Vector2 m = LeerMouse() * sensibilidad;
        yaw += m.x;
        pitch = Mathf.Clamp(pitch - m.y, -85f, 85f);
        jugador.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cabeza.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Mover()
    {
        Vector2 mov = LeerMovimiento();
        Vector3 dir = jugador.transform.right * mov.x + jugador.transform.forward * mov.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (cc.isGrounded && velY < 0f) velY = -2f;
        if (cc.isGrounded && LeerSalto()) velY = salto;
        velY += gravedad * Time.deltaTime;

        float vel = LeerCorrer() ? velocidadCorrer : velocidad;
        Vector3 total = dir * vel + Vector3.up * velY;
        cc.Move(total * Time.deltaTime);
    }

    void RevisarPuertas()
    {
        Vector3 p = jugador.transform.position;
        foreach (var d in puertas)
        {
            Vector3 rel = p - d.pos;
            float z = Vector3.Dot(rel, d.dentro);   // >0: dentro de la sala, <0: pasó la puerta
            float x = Vector3.Dot(rel, d.lado);
            if (z < distanciaCruce && z > -1f && Mathf.Abs(x) < anchoPuerta * 0.5f)
            {
                StartCoroutine(Cruzar(d));
                return;
            }
        }
    }

    IEnumerator Cruzar(Puerta usada)
    {
        transicion = true;

        // Conserva el ángulo con el que cruzaste, para que el giro sea continuo.
        float yawCruce = Mathf.Atan2(-usada.dentro.x, -usada.dentro.z) * Mathf.Rad2Deg;
        float rel = Mathf.Clamp(Mathf.DeltaAngle(yawCruce, yaw), -45f, 45f);

        for (float t = 0f; t < fundidoSalida; t += Time.deltaTime)
        {
            alfa = t / fundidoSalida;
            yield return null;
        }
        alfa = 1f;

        LimpiarMallas();
        Destroy(sala);
        yield return null;   // deja que Unity borre la sala vieja antes de crear la nueva

        if (vacio != null) vacio.Regenerar(Random.Range(0, int.MaxValue));
        ConstruirSala();
        ColocarJugador(entrada, rel);
        yield return null;

        for (float t = 0f; t < fundidoEntrada; t += Time.deltaTime)
        {
            alfa = 1f - t / fundidoEntrada;
            yield return null;
        }
        alfa = 0f;
        transicion = false;
    }

    // --------------------------------------------------------------------- UI

    void OnGUI()
    {
        if (alfa > 0.001f)
        {
            GUI.color = new Color(0f, 0f, 0f, alfa);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        if (Time.timeSinceLevelLoad < 12f)
            GUI.Label(new Rect(12f, 8f, 800f, 24f),
                "WASD mover  |  ratón mirar  |  Shift correr  |  Espacio saltar  |  Esc suelta el cursor  |  camina hacia una puerta");
    }

    // ------------------------------------------- entrada (sistema nuevo o viejo)

    Vector2 LeerMovimiento()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f)
                - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
        float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f)
                - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
        return new Vector2(x, y);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    Vector2 LeerMouse()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m == null) return Vector2.zero;
        return m.delta.ReadValue() * 0.1f;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    bool LeerCorrer()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    bool LeerSalto()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    bool LeerClick()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
