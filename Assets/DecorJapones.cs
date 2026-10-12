using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decoración japonesa para las salas jugables: textura de tatami, y detalles de interior
/// (postes, rodapiés, ventanas de shoji, vigas y faroles de papel) o de balcón exterior
/// (parapeto con balaustres y lucecitas, faroles de pie y marcos de laca roja en las puertas).
/// Todo se mete en un ConstructorMalla, así que cada sala solo suma un MeshRenderer sin colisión.
/// </summary>
public static class DecorJapones
{
    const int Madera = ConstructorMalla.Madera;
    const int Laca = ConstructorMalla.Laca;
    const int Papel = ConstructorMalla.Papel;

    // --------------------------------------------------------------- tatami

    /// <summary>
    /// Textura de una estera: 2 m x 1 m con fibras de junco a lo largo (eje U) y las
    /// franjas oscuras de tela (heri) en los dos lados largos. Se repite por el piso.
    /// </summary>
    public static Texture2D TexturaTatami()
    {
        const int W = 256;
        const int H = 128;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
        var px = new Color[W * H];

        Random.State estado = Random.state;
        Random.InitState(7);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float fibra = 0.5f + 0.5f * Mathf.Sin(y * 1.9f + Mathf.PerlinNoise(x * 0.05f, y * 0.4f) * 6f);
                float v = 0.78f + 0.12f * fibra + Random.value * 0.06f;
                Color c = new Color(0.55f * v + 0.08f, 0.62f * v + 0.06f, 0.30f * v, 1f);

                if (y < 6 || y >= H - 6)
                    c = new Color(0.07f, 0.09f, 0.06f, 1f);
                else if (x < 2 || x >= W - 2)
                    c = new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 1f);

                px[y * W + x] = c;
            }
        }
        Random.state = estado;

        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply(true);
        return tex;
    }

    // ------------------------------------------------------------- utilidades

    // Mismas orientaciones que las paredes de CastilloInfinito: 0 = +Z, 1 = -Z, 2 = +X, 3 = -X.
    static void Orientacion(int idx, out Vector3 dentro, out Vector3 lado, out bool enX)
    {
        switch (idx)
        {
            case 0:
                dentro = new Vector3(0f, 0f, -1f); lado = Vector3.right; enX = true;
                break;
            case 1:
                dentro = new Vector3(0f, 0f, 1f); lado = Vector3.right; enX = true;
                break;
            case 2:
                dentro = new Vector3(-1f, 0f, 0f); lado = Vector3.forward; enX = false;
                break;
            default:
                dentro = new Vector3(1f, 0f, 0f); lado = Vector3.forward; enX = false;
                break;
        }
    }

    static bool EnPuerta(float p, List<float> puertas, float radio)
    {
        foreach (float d in puertas)
        {
            if (Mathf.Abs(p - d) < radio) return true;
        }
        return false;
    }

    static Vector3 Pos(Vector3 cara, Vector3 lado, Vector3 dentro, float p, float prof, float y)
    {
        return cara + lado * p + dentro * prof + Vector3.up * y;
    }

    // Caja alineada con la pared: 'largo' va a lo largo de la pared y 'prof' hacia el interior.
    static Vector3 TamPared(bool enX, float largo, float alto, float prof)
    {
        return enX ? new Vector3(largo, alto, prof) : new Vector3(prof, alto, largo);
    }

    // --------------------------------------------------------------- interior

    /// <summary>
    /// Detalles de una sala interior. Devuelve las posiciones de los faroles de papel
    /// para que CastilloInfinito ponga una luz en cada uno.
    /// </summary>
    public static List<Vector3> DecorarSala(ConstructorMalla m, float w, float l, float h, bool cerrado,
                                            List<float>[] offs, float anchoPuerta, float espesor)
    {
        for (int idx = 0; idx < 4; idx++)
        {
            Vector3 dentro, lado;
            bool enX;
            Orientacion(idx, out dentro, out lado, out enX);
            float semi = enX ? l / 2f : w / 2f;
            float dim = enX ? w : l;
            Vector3 cara = -dentro * semi + dentro * (espesor / 2f);   // cara interior de la pared
            DecorarPared(m, cara, dentro, lado, enX, dim, h, offs[idx], anchoPuerta);
        }

        // Vigas del techo: rejilla (gotenjo) si hay techo, vigas sueltas si está abierto.
        float yv = h - 0.12f;
        if (cerrado)
        {
            for (float z = -l / 2f + 0.9f; z < l / 2f; z += 1.8f)
                m.Caja(Madera, new Vector3(0f, yv, z), new Vector3(w, 0.22f, 0.26f));
            for (float x = -w / 2f + 1.8f; x < w / 2f - 0.5f; x += 3.6f)
                m.Caja(Madera, new Vector3(x, yv, 0f), new Vector3(0.28f, 0.22f, l));
        }
        else
        {
            for (float z = -l / 2f + 2.7f; z < l / 2f - 1f; z += 5.4f)
                m.Caja(Madera, new Vector3(0f, yv, z), new Vector3(w, 0.3f, 0.35f));
        }

        // Faroles de papel colgados de las vigas.
        var faroles = new List<Vector3>();
        const float r = 0.45f;
        for (float z = -l / 2f + 2.7f; z < l / 2f - 1f; z += 5.4f)
        {
            float yL = Mathf.Min(yv - 1.4f, 3.8f);
            ArquitecturaJaponesa.Chochin(m, new Vector3(0f, yL, z), r);
            float yTope = yL + 1.4f * r + 0.6f;
            if (yv - yTope > 0.05f)
                m.Caja(Madera, new Vector3(0f, (yTope + yv) / 2f, z), new Vector3(0.04f, yv - yTope, 0.04f));
            faroles.Add(new Vector3(0f, yL, z));
        }

        // Faroles de pie (andon) en fila junto a las paredes de los lados, como en los pasillos del castillo.
        for (int s = -1; s <= 1; s += 2)
        {
            int paredIdx = s > 0 ? 2 : 3;   // 2 = pared en +X, 3 = pared en -X
            for (float z = -l / 2f + 2.25f; z < l / 2f - 1f; z += 4.5f)
            {
                if (EnPuerta(z, offs[paredIdx], anchoPuerta * 0.5f + 0.9f)) continue;
                Vector3 pie = new Vector3(s * (w / 2f - 0.7f), 0f, z);
                ArquitecturaJaponesa.Andon(m, pie);
                faroles.Add(new Vector3(pie.x, 0.4f, pie.z));
            }
        }
        return faroles;
    }

    static void DecorarPared(ConstructorMalla m, Vector3 cara, Vector3 dentro, Vector3 lado, bool enX,
                             float dim, float h, List<float> puertas, float anchoPuerta)
    {
        int n = Mathf.Max(2, Mathf.RoundToInt(dim / 1.8f));
        float bahia = dim / n;

        // Postes (hashira) cada ~1,8 m, menos junto a las puertas.
        for (int i = 0; i <= n; i++)
        {
            float p = -dim / 2f + bahia * i;
            if (EnPuerta(p, puertas, anchoPuerta * 0.5f + 0.15f)) continue;
            m.Caja(Madera, Pos(cara, lado, dentro, p, 0.1f, h / 2f), TamPared(enX, 0.24f, h, 0.2f));
        }

        // En cada tramo sin puerta: rodapié, viga alta y una ventana de shoji iluminada.
        for (int i = 0; i < n; i++)
        {
            float c = -dim / 2f + bahia * (i + 0.5f);
            if (EnPuerta(c, puertas, anchoPuerta * 0.5f + bahia * 0.5f - 0.1f)) continue;

            m.Caja(Madera, Pos(cara, lado, dentro, c, 0.07f, 0.17f), TamPared(enX, bahia, 0.18f, 0.14f));
            m.Caja(Madera, Pos(cara, lado, dentro, c, 0.07f, 2.8f), TamPared(enX, bahia, 0.2f, 0.14f));

            Matrix4x4 guardada = m.M;
            m.M = guardada * Matrix4x4.TRS(Pos(cara, lado, dentro, c, 0.04f, 1.75f),
                                           Quaternion.LookRotation(dentro, Vector3.up), Vector3.one);
            ArquitecturaJaponesa.Shoji(m, Vector3.zero, bahia - 0.3f, 1.9f);
            m.M = guardada;
        }
    }

    // ---------------------------------------------------------------- balcón

    /// <summary>
    /// Detalles de un balcón exterior (sala sin techo con parapeto bajo, como las galerías de las
    /// referencias): juntas de tablas en el piso, remate de madera con una hilera de lucecitas,
    /// balaustres, faroles de pie (andon) sobre el piso y un marco de laca roja alrededor de cada puerta.
    /// Devuelve la posición de los faroles para ponerles luz.
    /// </summary>
    public static List<Vector3> DecorarBalcon(ConstructorMalla m, float w, float l, float h,
                                              List<float>[] offs, float anchoPuerta, float espesor)
    {
        var faroles = new List<Vector3>();

        // Juntas entre las tablas del piso (un poco más oscuras que el piso).
        for (float x = -w / 2f + 0.5f; x < w / 2f - 0.1f; x += 0.5f)
            m.Caja(Madera, new Vector3(x, 0.012f, 0f), new Vector3(0.03f, 0.024f, l));
        for (float z = -l / 2f + 3.6f; z < l / 2f - 0.5f; z += 3.6f)
            m.Caja(Madera, new Vector3(0f, 0.012f, z), new Vector3(w, 0.024f, 0.03f));

        for (int idx = 0; idx < 4; idx++)
        {
            Vector3 dentro, lado;
            bool enX;
            Orientacion(idx, out dentro, out lado, out enX);
            float semi = enX ? l / 2f : w / 2f;
            float dim = enX ? w : l;
            Vector3 centro = -dentro * semi;
            Vector3 cara = centro + dentro * (espesor / 2f);   // cara interior del parapeto

            var puertas = new List<float>(offs[idx]);
            puertas.Sort();

            // Un tramo de parapeto entre cada par de puertas.
            float cursor = -dim / 2f - espesor / 2f;
            for (int k = 0; k <= puertas.Count; k++)
            {
                float fin = k < puertas.Count ? puertas[k] - anchoPuerta / 2f : dim / 2f + espesor / 2f;
                Parapeto(m, centro, cara, dentro, lado, enX, cursor, fin, h, faroles);
                if (k < puertas.Count) cursor = puertas[k] + anchoPuerta / 2f;
            }

            foreach (float d in puertas)
                Portico(m, centro + lado * d, dentro, lado, enX, anchoPuerta, faroles);
        }

        // Postes altos de laca roja en las cuatro esquinas.
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 c = new Vector3(sx * w / 2f, 0f, sz * l / 2f);
                m.Caja(Laca, c + Vector3.up * ((h + 0.9f) / 2f), new Vector3(0.34f, h + 0.9f, 0.34f));
            }
        }
        return faroles;
    }

    // Un tramo de parapeto: remate con lucecitas, largueros, balaustres y faroles de pie pegados al muro.
    static void Parapeto(ConstructorMalla m, Vector3 centro, Vector3 cara, Vector3 dentro, Vector3 lado,
                         bool enX, float desde, float hasta, float h, List<Vector3> faroles)
    {
        float len = hasta - desde;
        if (len < 0.2f) return;
        float medio = (desde + hasta) / 2f;

        // Remate de madera sobre el muro.
        m.Caja(Madera, centro + lado * medio + Vector3.up * (h + 0.06f), TamPared(enX, len, 0.12f, 0.62f));

        // Hilera de lucecitas sobre el remate, como las de las galerías del castillo.
        int nl = Mathf.Max(1, Mathf.RoundToInt(len / 0.9f));
        for (int i = 0; i < nl; i++)
        {
            float p = desde + len * (i + 0.5f) / nl;
            m.Caja(Papel, centro + lado * p + Vector3.up * (h + 0.18f), new Vector3(0.12f, 0.12f, 0.12f));
        }

        // Dos largueros sobre la cara interior y balaustres entre ambos.
        Vector3 fondo = cara + dentro * 0.05f;
        m.Caja(Madera, fondo + lado * medio + Vector3.up * 0.15f, TamPared(enX, len, 0.1f, 0.1f));
        m.Caja(Madera, fondo + lado * medio + Vector3.up * (h - 0.2f), TamPared(enX, len, 0.12f, 0.1f));

        int nb = Mathf.FloorToInt(len / 0.45f);
        float yb0 = 0.2f;
        float yb1 = h - 0.26f;
        if (nb > 0 && yb1 > yb0)
        {
            for (int i = 0; i < nb; i++)
            {
                float p = desde + len * (i + 0.5f) / nb;
                m.Caja(Madera, fondo + lado * p + Vector3.up * ((yb0 + yb1) / 2f),
                       TamPared(enX, 0.05f, yb1 - yb0, 0.05f));
            }
        }

        // Faroles de pie (andon) sobre el piso, junto al parapeto.
        int na = Mathf.Max(1, Mathf.RoundToInt(len / 3.2f));
        for (int i = 0; i < na; i++)
        {
            float p = desde + len * (i + 0.5f) / na;
            Vector3 b = cara + lado * p + dentro * 0.45f;
            ArquitecturaJaponesa.Andon(m, new Vector3(b.x, 0f, b.z));
            faroles.Add(new Vector3(b.x, 0.45f, b.z));
        }
    }

    // Marco de laca roja alrededor de una puerta (dos postes, dintel y una tabla encima) con un andon a cada lado.
    static void Portico(ConstructorMalla m, Vector3 pos, Vector3 dentro, Vector3 lado, bool enX,
                        float anchoPuerta, List<Vector3> faroles)
    {
        const float alto = 3.4f;
        float mitad = anchoPuerta / 2f + 0.1f;
        float anchoViga = anchoPuerta + 0.9f;

        for (int s = -1; s <= 1; s += 2)
            m.Caja(Laca, pos + lado * (s * mitad) + Vector3.up * (alto / 2f), TamPared(enX, 0.3f, alto, 0.5f));
        m.Caja(Laca, pos + Vector3.up * 3.2f, TamPared(enX, anchoViga, 0.4f, 0.5f));
        m.Caja(Madera, pos + Vector3.up * 3.45f, TamPared(enX, anchoViga + 0.5f, 0.1f, 0.8f));

        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 f = pos + lado * (s * (mitad + 0.55f)) + dentro * 0.5f;
            ArquitecturaJaponesa.Andon(m, new Vector3(f.x, 0f, f.z));
            faroles.Add(new Vector3(f.x, 0.45f, f.z));
        }
    }
}
