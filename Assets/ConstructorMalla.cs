using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Acumula geometría (cajas, cuadriláteros y techos curvos de teja) separada en
/// submallas, una por material, y la convierte en un único Mesh.
/// Es la base con la que ArquitecturaJaponesa dibuja casas, pagodas, torii, etc.
/// </summary>
public class ConstructorMalla
{
    // Índices de submalla = orden de los materiales.
    public const int Muro = 0;     // yeso blanco / crema
    public const int Madera = 1;   // madera oscura
    public const int Teja = 2;     // tejas gris azulado
    public const int Laca = 3;     // laca roja (torii, pagoda)
    public const int Piedra = 4;   // bases de piedra
    public const int Papel = 5;    // papel de shoji / linternas (con brillo)
    public const int NumSub = 6;

    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int>[] tris = new List<int>[NumSub];

    /// <summary>Transformación que se aplica a todo lo que se agrega a partir de ahora.</summary>
    public Matrix4x4 M = Matrix4x4.identity;

    public ConstructorMalla()
    {
        for (int i = 0; i < NumSub; i++) tris[i] = new List<int>();
    }

    // ------------------------------------------------------------ primitivas

    /// <summary>Cuadrilátero a-b-c-d; el lado visible es el que apunta hacia 'preferida'.</summary>
    public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 preferida)
    {
        a = M.MultiplyPoint3x4(a);
        b = M.MultiplyPoint3x4(b);
        c = M.MultiplyPoint3x4(c);
        d = M.MultiplyPoint3x4(d);
        Vector3 pref = M.MultiplyVector(preferida);

        Vector3 n = Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a);
        int i = verts.Count;
        verts.Add(a);
        verts.Add(b);
        verts.Add(c);
        verts.Add(d);

        List<int> t = tris[sub];
        if (Vector3.Dot(n, pref) >= 0f)
        {
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }
        else
        {
            t.Add(i); t.Add(i + 2); t.Add(i + 1);
            t.Add(i); t.Add(i + 3); t.Add(i + 2);
        }
    }

    public void Caja(int sub, Vector3 centro, Vector3 tam)
    {
        Vector3 h = tam * 0.5f;
        Vector3 p000 = centro + new Vector3(-h.x, -h.y, -h.z);
        Vector3 p100 = centro + new Vector3(h.x, -h.y, -h.z);
        Vector3 p110 = centro + new Vector3(h.x, h.y, -h.z);
        Vector3 p010 = centro + new Vector3(-h.x, h.y, -h.z);
        Vector3 p001 = centro + new Vector3(-h.x, -h.y, h.z);
        Vector3 p101 = centro + new Vector3(h.x, -h.y, h.z);
        Vector3 p111 = centro + new Vector3(h.x, h.y, h.z);
        Vector3 p011 = centro + new Vector3(-h.x, h.y, h.z);

        Quad(sub, p001, p101, p111, p011, Vector3.forward);
        Quad(sub, p100, p000, p010, p110, Vector3.back);
        Quad(sub, p101, p100, p110, p111, Vector3.right);
        Quad(sub, p000, p001, p011, p010, Vector3.left);
        Quad(sub, p011, p111, p110, p010, Vector3.up);
        Quad(sub, p000, p100, p101, p001, Vector3.down);
    }

    public void CajaRot(int sub, Vector3 centro, Vector3 tam, Quaternion rot)
    {
        Matrix4x4 guardada = M;
        M = M * Matrix4x4.TRS(centro, rot, Vector3.one);
        Caja(sub, Vector3.zero, tam);
        M = guardada;
    }

    // ----------------------------------------------------------------- techo

    /// <summary>
    /// Techo japonés a cuatro aguas (yosemune) con tejas, pendiente cóncava, aleros anchos
    /// que se levantan en las esquinas, cumbrera y remates. 'centro' es el punto del alero
    /// a nivel medio; 'ancho' x 'largo' es el muro que cubre; 'vuelo' es cuánto sobresale.
    /// Si ancho = largo queda en pirámide (pagodas y torres).
    /// </summary>
    public void Techo(Vector3 centro, float ancho, float largo, float alto, float vuelo, float alza)
    {
        Matrix4x4 guardada = M;
        M = M * Matrix4x4.TRS(centro, Quaternion.identity, Vector3.one);

        float ex = ancho * 0.5f + vuelo;
        float ez = largo * 0.5f + vuelo;
        if (ex < ez)
        {
            // Trabaja siempre con la cumbrera a lo largo de X: gira 90 grados y cambia ejes.
            M = M * Matrix4x4.Rotate(Quaternion.Euler(0f, 90f, 0f));
            float tmp = ex;
            ex = ez;
            ez = tmp;
        }
        float rx = ex - ez;   // media longitud de la cumbrera

        CaraTecho(new Vector2(-ex, ez), new Vector2(ex, ez), new Vector2(-rx, 0f), new Vector2(rx, 0f), alto, alza);
        CaraTecho(new Vector2(ex, ez), new Vector2(ex, -ez), new Vector2(rx, 0f), new Vector2(rx, 0f), alto, alza);
        CaraTecho(new Vector2(ex, -ez), new Vector2(-ex, -ez), new Vector2(rx, 0f), new Vector2(-rx, 0f), alto, alza);
        CaraTecho(new Vector2(-ex, -ez), new Vector2(-ex, ez), new Vector2(-rx, 0f), new Vector2(-rx, 0f), alto, alza);

        // Cumbrera y remates (onigawara).
        Caja(Teja, new Vector3(0f, alto + 0.1f, 0f), new Vector3(2f * rx + 0.6f, 0.3f, 0.45f));
        if (rx > 0.01f)
        {
            Caja(Teja, new Vector3(rx + 0.25f, alto + 0.35f, 0f), new Vector3(0.45f, 0.8f, 0.5f));
            Caja(Teja, new Vector3(-rx - 0.25f, alto + 0.35f, 0f), new Vector3(0.45f, 0.8f, 0.5f));
        }
        else
        {
            Caja(Teja, new Vector3(0f, alto + 0.4f, 0f), new Vector3(0.5f, 0.7f, 0.5f));
        }

        M = guardada;
    }

    // Una de las cuatro caras del techo: parte del borde del alero (e0 a e1) y sube a la cumbrera (r0 a r1).
    void CaraTecho(Vector2 e0, Vector2 e1, Vector2 r0, Vector2 r1, float alto, float alza)
    {
        const int nu = 10;     // divisiones a lo largo del alero
        const int nv = 6;      // divisiones a lo largo de la pendiente
        const float grosor = 0.25f;

        Vector3[,] sup = new Vector3[nu + 1, nv + 1];
        for (int i = 0; i <= nu; i++)
        {
            float s = i / (float)nu;
            Vector2 pe = Vector2.Lerp(e0, e1, s);
            Vector2 pr = Vector2.Lerp(r0, r1, s);
            float u = 2f * s - 1f;
            float borde = alza * Mathf.Pow(Mathf.Abs(u), 4f);   // las esquinas del alero se curvan hacia arriba
            for (int j = 0; j <= nv; j++)
            {
                float t = j / (float)nv;
                Vector2 p = Vector2.Lerp(pe, pr, t);
                float y = alto * Mathf.Pow(t, 1.35f) + borde * (1f - t) * (1f - t);
                sup[i, j] = new Vector3(p.x, y, p.y);
            }
        }

        Vector3 abajo = new Vector3(0f, -grosor, 0f);
        for (int i = 0; i < nu; i++)
        {
            for (int j = 0; j < nv; j++)
            {
                Vector3 a = sup[i, j];
                Vector3 b = sup[i + 1, j];
                Vector3 c = sup[i + 1, j + 1];
                Vector3 d = sup[i, j + 1];
                Quad(Teja, a, b, c, d, Vector3.up);
                Quad(Madera, a + abajo, b + abajo, c + abajo, d + abajo, Vector3.down);
            }

            // Canto del alero.
            Vector3 p0 = sup[i, 0];
            Vector3 p1 = sup[i + 1, 0];
            Vector3 medio = (p0 + p1) * 0.5f;
            Quad(Madera, p0, p1, p1 + abajo, p0 + abajo, new Vector3(medio.x, 0f, medio.z));
        }
    }

    // -------------------------------------------------------------- resultado

    public Mesh Construir(string nombre)
    {
        var mesh = new Mesh();
        mesh.name = nombre;
        if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.subMeshCount = NumSub;
        for (int i = 0; i < NumSub; i++) mesh.SetTriangles(tris[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
