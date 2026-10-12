using UnityEngine;

/// <summary>
/// Estructuras del vacío según las imágenes del castillo: volúmenes de madera apilados y desplazados,
/// con techo plano, franjas de ventanas shoji (unas encendidas y otras apagadas), pilares de laca roja,
/// balcones con barandilla y hileras de lucecitas, cabinas que sobresalen, plataformas de tatami con
/// faroles de pie, escaleras largas y vigas gigantes. Ninguna lleva techo de teja a cuatro aguas.
/// El pivote de cada malla queda en su centro.
/// </summary>
public static class TorresCastillo
{
    const int Muro = ConstructorMalla.Muro;       // papel apagado y tatami (sin brillo)
    const int Madera = ConstructorMalla.Madera;
    const int Negro = ConstructorMalla.Teja;      // laca negra: escaleras y barandillas
    const int Laca = ConstructorMalla.Laca;
    const int Papel = ConstructorMalla.Papel;     // papel iluminado (con brillo)

    static float Rango(System.Random r, float a, float b)
    {
        return a + (float)r.NextDouble() * (b - a);
    }

    // --------------------------------------------------------------- piezas

    // Franja de ventanas shoji sobre una cara. Se dibuja en el espacio local de m.M: el origen es el
    // centro de la cara a nivel del piso y +Z apunta hacia afuera. Cada ventana se enciende o no al azar.
    static void Ventanas(ConstructorMalla m, System.Random rnd, float ancho, float alto, float probLuz)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(ancho / 1.5f));
        float bw = ancho / n;
        float hv = Mathf.Min(2.1f, alto - 0.9f);
        float yv = alto * 0.5f;

        for (int i = 0; i < n; i++)
        {
            float x = -ancho / 2f + bw * (i + 0.5f);
            int sub = rnd.NextDouble() < probLuz ? Papel : Muro;
            m.Caja(sub, new Vector3(x, yv, 0.03f), new Vector3(bw - 0.12f, hv, 0.06f));
        }

        for (int i = 0; i <= n; i++)
        {
            float x = -ancho / 2f + bw * i;
            m.Caja(Madera, new Vector3(x, yv, 0.07f), new Vector3(0.12f, hv + 0.1f, 0.12f));
            if (i > 0 && i < n && i % 3 == 0)
                m.Caja(Laca, new Vector3(x, alto / 2f, 0.14f), new Vector3(0.26f, alto, 0.26f));
        }

        m.Caja(Madera, new Vector3(0f, yv, 0.07f), new Vector3(ancho, 0.07f, 0.09f));                    // travesaño
        m.Caja(Madera, new Vector3(0f, yv - hv / 2f - 0.07f, 0.08f), new Vector3(ancho, 0.14f, 0.16f));  // antepecho
        m.Caja(Madera, new Vector3(0f, yv + hv / 2f + 0.07f, 0.08f), new Vector3(ancho, 0.14f, 0.16f));  // dintel
    }

    // Barandilla rectangular alrededor de 'c' (a nivel del piso), con postes y una hilera de lucecitas.
    static void Barandilla(ConstructorMalla m, Vector3 c, float ex, float ez)
    {
        Matrix4x4 guardada = m.M;
        for (int k = 0; k < 4; k++)
        {
            Vector3 n = k == 0 ? Vector3.forward : k == 1 ? Vector3.back : k == 2 ? Vector3.right : Vector3.left;
            float ancho = k < 2 ? 2f * ex : 2f * ez;
            float prof = k < 2 ? ez : ex;
            m.M = guardada * Matrix4x4.TRS(c + n * prof, Quaternion.LookRotation(n, Vector3.up), Vector3.one);

            m.Caja(Madera, new Vector3(0f, 1.0f, 0f), new Vector3(ancho, 0.1f, 0.14f));
            m.Caja(Madera, new Vector3(0f, 0.5f, 0f), new Vector3(ancho, 0.07f, 0.08f));

            int np = Mathf.Max(1, Mathf.RoundToInt(ancho / 3f));
            for (int i = 0; i <= np; i++)
                m.Caja(Madera, new Vector3(-ancho / 2f + ancho * i / np, 0.5f, 0f), new Vector3(0.14f, 1.0f, 0.14f));

            int nl = Mathf.Max(1, Mathf.RoundToInt(ancho / 1.5f));
            for (int i = 0; i < nl; i++)
                m.Caja(Papel, new Vector3(-ancho / 2f + ancho * (i + 0.5f) / nl, 1.13f, 0f), new Vector3(0.14f, 0.14f, 0.14f));
        }
        m.M = guardada;
    }

    // Techo plano con alero corto y tatami encima, como las azoteas de las imágenes.
    static void Azotea(ConstructorMalla m, Vector3 c, float a, float b, float y)
    {
        m.Caja(Madera, new Vector3(c.x, y + 0.125f, c.z), new Vector3(a + 1f, 0.25f, b + 1f));
        m.Caja(Muro, new Vector3(c.x, y + 0.28f, c.z), new Vector3(a + 0.6f, 0.06f, b + 0.6f));
        for (float x = -a / 2f + 1.8f; x < a / 2f - 0.5f; x += 1.8f)
            m.Caja(Madera, new Vector3(c.x + x, y + 0.31f, c.z), new Vector3(0.05f, 0.03f, b + 0.6f));
        for (float z = -b / 2f + 1.8f; z < b / 2f - 0.5f; z += 1.8f)
            m.Caja(Madera, new Vector3(c.x, y + 0.31f, c.z + z), new Vector3(a + 0.6f, 0.03f, 0.05f));
    }

    // ----------------------------------------------------------------- torre

    public static Mesh Torre(int pisos, float ancho, float largo, int semilla)
    {
        var m = new ConstructorMalla();
        var rnd = new System.Random(semilla);
        const float alto = 3.6f;

        float y = -pisos * alto / 2f;
        float cx = 0f;
        float cz = 0f;
        float a = ancho;
        float b = largo;
        for (int i = 0; i < pisos; i++)
        {
            a = ancho * Rango(rnd, 0.75f, 1.2f);
            b = largo * Rango(rnd, 0.75f, 1.2f);
            cx = Mathf.Clamp(cx + Rango(rnd, -2f, 2f), -ancho * 0.3f, ancho * 0.3f);
            cz = Mathf.Clamp(cz + Rango(rnd, -2f, 2f), -largo * 0.3f, largo * 0.3f);
            Piso(m, rnd, new Vector3(cx, y, cz), a, b, alto);
            y += alto;
        }

        Azotea(m, new Vector3(cx, y, cz), a, b, y);
        return m.Construir("Torre");
    }

    // Un piso: cuerpo de madera, ventanas shoji por las cuatro caras, pilares de laca, a veces una cabina
    // que sobresale, y un balcón con barandilla, lucecitas y faroles de pie en las esquinas.
    static void Piso(ConstructorMalla m, System.Random rnd, Vector3 c, float a, float b, float alto)
    {
        float ya = c.y;
        float probLuz = Rango(rnd, 0.15f, 0.85f);   // hay pisos casi apagados y pisos muy encendidos

        m.Caja(Madera, new Vector3(c.x, ya + alto / 2f, c.z), new Vector3(a, alto, b));

        // Pilares de laca roja en las cuatro esquinas.
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
                m.Caja(Laca, new Vector3(c.x + sx * a / 2f, ya + alto / 2f, c.z + sz * b / 2f), new Vector3(0.32f, alto, 0.32f));
        }

        Matrix4x4 guardada = m.M;
        for (int k = 0; k < 4; k++)
        {
            Vector3 n = k == 0 ? Vector3.forward : k == 1 ? Vector3.back : k == 2 ? Vector3.right : Vector3.left;
            float anchoCara = k < 2 ? a : b;
            float profCara = k < 2 ? b : a;
            Vector3 origen = new Vector3(c.x, ya, c.z) + n * (profCara / 2f);
            m.M = guardada * Matrix4x4.TRS(origen, Quaternion.LookRotation(n, Vector3.up), Vector3.one);
            Ventanas(m, rnd, anchoCara, alto, probLuz);

            // Cabina volada: una habitación que sobresale de la fachada y se superpone con el piso.
            if (rnd.NextDouble() < 0.3 && anchoCara > 6f)
            {
                float cw = Rango(rnd, 3f, Mathf.Min(7f, anchoCara * 0.6f));
                float cd = Rango(rnd, 2.5f, 5f);
                float cxl = Rango(rnd, -anchoCara * 0.25f, anchoCara * 0.25f);
                float ch = alto - 0.4f;
                m.Caja(Madera, new Vector3(cxl, 0.3f + ch / 2f, cd / 2f), new Vector3(cw, ch, cd));

                Matrix4x4 dentroDeCara = m.M;
                m.M = dentroDeCara * Matrix4x4.TRS(new Vector3(cxl, 0.3f, cd), Quaternion.identity, Vector3.one);
                Ventanas(m, rnd, cw, ch, 0.8f);
                m.M = dentroDeCara;
            }
            m.M = guardada;
        }

        // Balcón: losa que sobresale 1,2 m, barandilla con lucecitas y un farol de pie en cada esquina.
        float ex = a / 2f + 1.2f;
        float ez = b / 2f + 1.2f;
        m.Caja(Madera, new Vector3(c.x, ya + 0.11f, c.z), new Vector3(2f * ex, 0.22f, 2f * ez));
        Barandilla(m, new Vector3(c.x, ya + 0.22f, c.z), ex, ez);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
                ArquitecturaJaponesa.Andon(m, new Vector3(c.x + sx * (ex - 0.45f), ya + 0.22f, c.z + sz * (ez - 0.45f)));
        }
    }

    // ------------------------------------------------------------ plataforma

    /// <summary>Plataforma de tatami con barandilla, faroles de pie y, a veces, una cabina al fondo.</summary>
    public static Mesh Plataforma(float ancho, float largo, int semilla)
    {
        var m = new ConstructorMalla();
        var rnd = new System.Random(semilla);

        m.Caja(Madera, new Vector3(0f, -0.25f, 0f), new Vector3(ancho, 0.5f, largo));
        m.Caja(Muro, new Vector3(0f, 0.015f, 0f), new Vector3(ancho - 0.5f, 0.03f, largo - 0.5f));
        for (float x = -ancho / 2f + 1.8f; x < ancho / 2f - 0.5f; x += 1.8f)
            m.Caja(Madera, new Vector3(x, 0.04f, 0f), new Vector3(0.05f, 0.03f, largo - 0.5f));
        for (float z = -largo / 2f + 1.8f; z < largo / 2f - 0.5f; z += 1.8f)
            m.Caja(Madera, new Vector3(0f, 0.04f, z), new Vector3(ancho - 0.5f, 0.03f, 0.05f));

        float ex = ancho / 2f - 0.15f;
        float ez = largo / 2f - 0.15f;
        Barandilla(m, new Vector3(0f, 0.03f, 0f), ex, ez);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                m.Caja(Laca, new Vector3(sx * ex, 0.73f, sz * ez), new Vector3(0.3f, 1.4f, 0.3f));
                ArquitecturaJaponesa.Andon(m, new Vector3(sx * (ex - 0.7f), 0.03f, sz * (ez - 0.7f)));
            }
        }
        for (float x = -ex + 2.5f; x < ex - 1f; x += 3.6f)
        {
            ArquitecturaJaponesa.Andon(m, new Vector3(x, 0.03f, ez - 0.7f));
            ArquitecturaJaponesa.Andon(m, new Vector3(x, 0.03f, -ez + 0.7f));
        }

        // Cabina con ventanas al fondo, con la fachada hacia el centro de la plataforma.
        if (rnd.NextDouble() < 0.7)
        {
            float cw = ancho * Rango(rnd, 0.5f, 0.8f);
            float cd = 3f;
            float zf = -largo / 2f + 0.5f + cd;
            const float hc = 3.4f;
            m.Caja(Madera, new Vector3(0f, hc / 2f, zf - cd / 2f), new Vector3(cw, hc, cd));

            Matrix4x4 guardada = m.M;
            m.M = guardada * Matrix4x4.TRS(new Vector3(0f, 0.03f, zf), Quaternion.identity, Vector3.one);
            Ventanas(m, rnd, cw, hc, 0.85f);
            m.M = guardada;
            Azotea(m, new Vector3(0f, 0f, zf - cd / 2f), cw, cd, hc);
        }
        return m.Construir("Plataforma");
    }

    // -------------------------------------------------------------- escalera

    /// <summary>Escalera de madera suspendida con largueros y barandillas negras y lucecitas. Sube hacia +X.</summary>
    public static Mesh EscaleraBaranda(int peldanos, float ancho)
    {
        var m = new ConstructorMalla();
        const float alzada = 0.22f;
        const float huella = 0.45f;
        float run = peldanos * huella;
        float rise = peldanos * alzada;
        m.M = Matrix4x4.Translate(new Vector3(-run / 2f, -rise / 2f, 0f));

        for (int i = 0; i < peldanos; i++)
        {
            m.Caja(Madera, new Vector3(i * huella + huella / 2f, (i + 1) * alzada - 0.06f, 0f),
                   new Vector3(huella, 0.12f, ancho));
        }

        float largo = Mathf.Sqrt(run * run + rise * rise);
        float ang = Mathf.Atan2(alzada, huella) * Mathf.Rad2Deg;
        for (int s = -1; s <= 1; s += 2)
        {
            float z = s * ancho / 2f;
            m.CajaRot(Negro, new Vector3(run / 2f, rise / 2f - 0.15f, z), new Vector3(largo, 0.4f, 0.14f), Quaternion.Euler(0f, 0f, ang));
            m.CajaRot(Negro, new Vector3(run / 2f, rise / 2f + 0.9f, z), new Vector3(largo, 0.08f, 0.1f), Quaternion.Euler(0f, 0f, ang));
            for (int k = 0; k < peldanos; k += 4)
            {
                float px = k * huella + huella / 2f;
                float py = (k + 1) * alzada;
                m.Caja(Negro, new Vector3(px, py + 0.45f, z), new Vector3(0.08f, 0.9f, 0.08f));
                m.Caja(Papel, new Vector3(px, py + 1.02f, z), new Vector3(0.16f, 0.16f, 0.16f));
            }
        }
        return m.Construir("EscaleraBaranda");
    }

    // ------------------------------------------------------------------ viga

    /// <summary>Viga gigante de laca roja con abrazaderas, una escalera de mano y lucecitas. Va a lo largo de X.</summary>
    public static Mesh VigaGigante(float largo)
    {
        var m = new ConstructorMalla();
        float g = Mathf.Clamp(largo / 25f, 1.6f, 4f);

        m.Caja(Laca, Vector3.zero, new Vector3(largo, g, g));
        for (float x = -largo / 2f + 2f; x < largo / 2f - 1f; x += 4f)
            m.Caja(Madera, new Vector3(x, 0f, 0f), new Vector3(0.4f, g + 0.5f, g + 0.5f));

        // Escalera de mano pegada a una cara: dos largueros y peldaños cada metro.
        float zl = g / 2f + 0.05f;
        m.Caja(Negro, new Vector3(0f, g * 0.2f, zl), new Vector3(largo, 0.08f, 0.08f));
        m.Caja(Negro, new Vector3(0f, -g * 0.2f, zl), new Vector3(largo, 0.08f, 0.08f));
        for (float x = -largo / 2f + 0.5f; x < largo / 2f; x += 1f)
            m.Caja(Negro, new Vector3(x, 0f, zl), new Vector3(0.07f, g * 0.4f, 0.07f));

        for (float x = -largo / 2f + 3f; x < largo / 2f; x += 6f)
            m.Caja(Papel, new Vector3(x, g / 2f + 0.15f, 0f), new Vector3(0.3f, 0.3f, 0.3f));
        return m.Construir("VigaGigante");
    }
}
