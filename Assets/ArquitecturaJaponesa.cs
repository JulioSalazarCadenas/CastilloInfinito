using UnityEngine;

/// <summary>
/// Estructuras de estilo japonés tradicional generadas por código: casas con engawa y
/// shoji, pagodas, puertas torii, corredores techados, escaleras y linternas.
/// Cada una devuelve un Mesh con 6 submallas (ver ConstructorMalla): yeso, madera,
/// tejas, laca roja, piedra y papel de shoji. Las medidas están en metros y siguen
/// proporciones reales: postes cada ~1,8 m (un tatami), engawa de ~1,2 m de ancho,
/// aleros muy salidos con las esquinas curvadas hacia arriba y pagodas cuyos pisos
/// se hacen más pequeños hacia arriba.
/// </summary>
public static class ArquitecturaJaponesa
{
    const int Muro = ConstructorMalla.Muro;
    const int Madera = ConstructorMalla.Madera;
    const int Laca = ConstructorMalla.Laca;
    const int Piedra = ConstructorMalla.Piedra;
    const int Papel = ConstructorMalla.Papel;

    // --------------------------------------------------------- piezas sueltas

    /// <summary>Panel shoji que mira hacia +Z: papel iluminado con rejilla de listones (kumiko).</summary>
    public static void Shoji(ConstructorMalla m, Vector3 centro, float ancho, float alto)
    {
        m.Caja(Papel, centro, new Vector3(ancho, alto, 0.03f));

        int nv = Mathf.Max(2, Mathf.RoundToInt(ancho / 0.45f));
        int nh = Mathf.Max(2, Mathf.RoundToInt(alto / 0.45f));
        for (int i = 0; i <= nv; i++)
        {
            float x = -ancho / 2f + ancho * i / nv;
            m.Caja(Madera, centro + new Vector3(x, 0f, 0.02f), new Vector3(0.05f, alto, 0.05f));
        }
        for (int j = 0; j <= nh; j++)
        {
            float y = -alto / 2f + alto * j / nh;
            m.Caja(Madera, centro + new Vector3(0f, y, 0.02f), new Vector3(ancho, 0.05f, 0.05f));
        }
    }

    /// <summary>Farol de papel colgante (chochin) con tapas de laca y cuerda.</summary>
    public static void Chochin(ConstructorMalla m, Vector3 pos, float r)
    {
        m.Caja(Papel, pos, new Vector3(1.8f * r, 2.2f * r, 1.8f * r));
        m.Caja(Laca, pos + new Vector3(0f, 1.2f * r, 0f), new Vector3(1.2f * r, 0.25f * r, 1.2f * r));
        m.Caja(Laca, pos - new Vector3(0f, 1.2f * r, 0f), new Vector3(1.2f * r, 0.25f * r, 1.2f * r));
        m.Caja(Madera, pos + new Vector3(0f, 1.4f * r + 0.3f, 0f), new Vector3(0.04f, 0.6f, 0.04f));
    }

    /// <summary>
    /// Farol de pie (andon): caja de papel iluminada con marco de madera, de ~0,8 m. Es el farol
    /// que se ve en filas a lo largo de los pasillos y balcones del castillo. 'pos' es el punto del suelo.
    /// </summary>
    public static void Andon(ConstructorMalla m, Vector3 pos, float escala = 1f)
    {
        float s = escala;
        m.Caja(Madera, pos + new Vector3(0f, 0.03f * s, 0f), new Vector3(0.5f * s, 0.06f * s, 0.5f * s));
        m.Caja(Papel, pos + new Vector3(0f, 0.41f * s, 0f), new Vector3(0.36f * s, 0.7f * s, 0.36f * s));
        m.Caja(Madera, pos + new Vector3(0f, 0.79f * s, 0f), new Vector3(0.46f * s, 0.06f * s, 0.46f * s));
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                m.Caja(Madera, pos + new Vector3(sx * 0.19f * s, 0.41f * s, sz * 0.19f * s),
                       new Vector3(0.04f * s, 0.7f * s, 0.04f * s));
            }
        }
    }

    // ------------------------------------------------------------------ casa

    /// <summary>
    /// Casa japonesa: base de piedra, muros de yeso con postes y vigas de madera oscura,
    /// frente de shoji, engawa, techo a cuatro aguas y, si pisos > 1, un segundo piso
    /// más pequeño con su faldón de teja. El frente mira hacia +Z.
    /// </summary>
    public static Mesh Casa(float a, float b, int pisos)
    {
        var m = new ConstructorMalla();
        const float baseAlto = 0.5f;
        const float hMuro = 2.9f;
        const float engawaAncho = 1.2f;

        m.Caja(Piedra, new Vector3(0f, baseAlto / 2f, engawaAncho / 2f),
               new Vector3(a + 2.4f, baseAlto, b + engawaAncho + 0.8f));
        // Piedra de entrada delante del engawa.
        m.Caja(Piedra, new Vector3(0f, 0.15f, b / 2f + engawaAncho + 1.1f), new Vector3(1.6f, 0.3f, 0.9f));

        float y = baseAlto;
        float aa = a;
        float bb = b;
        for (int i = 0; i < pisos; i++)
        {
            Piso(m, aa, bb, y, hMuro, i == 0);
            float yTecho = y + hMuro + 0.1f;
            if (i == pisos - 1)
                m.Techo(new Vector3(0f, yTecho, 0f), aa, bb, 2.3f, 1.7f, 0.55f);
            else
                m.Techo(new Vector3(0f, yTecho, 0f), aa, bb, 0.9f, 1.3f, 0.3f);

            y = yTecho;     // el piso siguiente atraviesa el faldón del techo
            aa *= 0.78f;
            bb *= 0.78f;
        }
        return m.Construir("Casa");
    }

    static void Piso(ConstructorMalla m, float a, float b, float y0, float hMuro, bool engawa)
    {
        const float t = 0.2f;
        float yc = y0 + hMuro / 2f;

        // Muros trasero e izquierdo/derecho.
        m.Caja(Muro, new Vector3(0f, yc, -b / 2f + t / 2f), new Vector3(a, hMuro, t));
        m.Caja(Muro, new Vector3(-a / 2f + t / 2f, yc, 0f), new Vector3(t, hMuro, b));
        m.Caja(Muro, new Vector3(a / 2f - t / 2f, yc, 0f), new Vector3(t, hMuro, b));
        m.Caja(Madera, new Vector3(0f, y0 - 0.04f, 0f), new Vector3(a, 0.1f, b));   // piso interior

        int nx = Mathf.Max(2, Mathf.RoundToInt(a / 1.8f));
        int nz = Mathf.Max(2, Mathf.RoundToInt(b / 1.8f));

        // Postes (hashira).
        for (int i = 0; i <= nx; i++)
        {
            float x = -a / 2f + a * i / nx;
            m.Caja(Madera, new Vector3(x, yc, b / 2f), new Vector3(0.24f, hMuro, 0.24f));
            m.Caja(Madera, new Vector3(x, yc, -b / 2f), new Vector3(0.24f, hMuro, 0.24f));
        }
        for (int k = 1; k < nz; k++)
        {
            float z = -b / 2f + b * k / nz;
            m.Caja(Madera, new Vector3(-a / 2f, yc, z), new Vector3(0.24f, hMuro, 0.24f));
            m.Caja(Madera, new Vector3(a / 2f, yc, z), new Vector3(0.24f, hMuro, 0.24f));
        }

        // Vigas horizontales (nuki) abajo y arriba en los cuatro lados.
        foreach (float vy in new float[] { y0 + 0.12f, y0 + hMuro - 0.15f })
        {
            m.Caja(Madera, new Vector3(0f, vy, b / 2f), new Vector3(a + 0.24f, 0.2f, 0.26f));
            m.Caja(Madera, new Vector3(0f, vy, -b / 2f), new Vector3(a + 0.24f, 0.2f, 0.26f));
            m.Caja(Madera, new Vector3(-a / 2f, vy, 0f), new Vector3(0.26f, 0.2f, b));
            m.Caja(Madera, new Vector3(a / 2f, vy, 0f), new Vector3(0.26f, 0.2f, b));
        }

        // Frente de shoji entre los postes.
        float altoShoji = hMuro - 0.55f;
        for (int i = 0; i < nx; i++)
        {
            float cx = -a / 2f + a * (i + 0.5f) / nx;
            Shoji(m, new Vector3(cx, y0 + 0.25f + altoShoji / 2f, b / 2f - 0.04f), a / nx - 0.24f, altoShoji);
        }

        // Engawa: tarima de madera que sobresale del frente y de los lados.
        if (engawa)
            m.Caja(Madera, new Vector3(0f, y0 - 0.04f, b / 2f + 0.6f), new Vector3(a + 1.8f, 0.1f, 1.2f));
    }

    // ---------------------------------------------------------------- pagoda

    /// <summary>Pagoda de 'niveles' pisos (3 o 5): cada piso es más pequeño, con techo piramidal y remate.</summary>
    public static Mesh Pagoda(int niveles, float s0)
    {
        var m = new ConstructorMalla();
        const float baseAlto = 0.9f;
        m.Caja(Piedra, new Vector3(0f, baseAlto / 2f, 0f), new Vector3(s0 + 3.4f, baseAlto, s0 + 3.4f));

        float y = baseAlto;
        float s = s0;
        float yTecho = y;
        for (int n = 0; n < niveles; n++)
        {
            const float h = 2.6f;
            m.Caja(Muro, new Vector3(0f, y + h / 2f, 0f), new Vector3(s - 0.5f, h, s - 0.5f));

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                    m.Caja(Laca, new Vector3(sx * s / 2f, y + h / 2f, sz * s / 2f), new Vector3(0.35f, h, 0.35f));
            }
            foreach (float vy in new float[] { y + 0.15f, y + h - 0.15f })
            {
                m.Caja(Laca, new Vector3(0f, vy, s / 2f), new Vector3(s + 0.3f, 0.25f, 0.3f));
                m.Caja(Laca, new Vector3(0f, vy, -s / 2f), new Vector3(s + 0.3f, 0.25f, 0.3f));
                m.Caja(Laca, new Vector3(s / 2f, vy, 0f), new Vector3(0.3f, 0.25f, s));
                m.Caja(Laca, new Vector3(-s / 2f, vy, 0f), new Vector3(0.3f, 0.25f, s));
            }

            // Ventana de shoji en cada una de las cuatro caras.
            Matrix4x4 guardada = m.M;
            for (int r = 0; r < 4; r++)
            {
                m.M = guardada * Matrix4x4.Rotate(Quaternion.Euler(0f, 90f * r, 0f));
                Shoji(m, new Vector3(0f, y + h / 2f, s / 2f - 0.2f), s * 0.45f, h * 0.55f);
            }
            m.M = guardada;

            yTecho = y + h + 0.1f;
            m.Techo(new Vector3(0f, yTecho, 0f), s, s, 1.2f, 1.9f - 0.08f * n, 0.6f);
            y = yTecho;
            s *= 0.87f;
        }

        // Remate (sorin): mástil con anillos.
        float yMastil = yTecho + 1.2f;
        m.Caja(Madera, new Vector3(0f, yMastil + 2.0f, 0f), new Vector3(0.22f, 4.4f, 0.22f));
        for (int k = 0; k < 7; k++)
            m.Caja(Laca, new Vector3(0f, yMastil + 0.9f + k * 0.5f, 0f), new Vector3(0.8f - 0.09f * k, 0.12f, 0.8f - 0.09f * k));
        return m.Construir("Pagoda");
    }

    // ----------------------------------------------------------------- torii

    /// <summary>Puerta torii de laca roja con dintel negro de puntas levantadas. Se cruza por el eje Z.</summary>
    public static Mesh Torii(float alto, float ancho)
    {
        var m = new ConstructorMalla();
        float dx = ancho / 2f;
        for (int s = -1; s <= 1; s += 2)
        {
            m.Caja(Piedra, new Vector3(s * dx, 0.15f, 0f), new Vector3(1.0f, 0.3f, 1.0f));
            m.Caja(Laca, new Vector3(s * dx, alto / 2f, 0f), new Vector3(0.64f, alto, 0.64f));
        }
        m.Caja(Laca, new Vector3(0f, alto * 0.72f, 0f), new Vector3(ancho + 0.5f, 0.3f, 0.3f));       // nuki

        float anchoK = ancho + 2.4f;
        m.Caja(Laca, new Vector3(0f, alto - 0.08f, 0f), new Vector3(anchoK * 0.85f, 0.28f, 0.6f));    // shimaki

        float l0 = anchoK * 0.5f;                  // tramo central del kasagi
        float l1 = anchoK * 0.25f;                 // tramos de las puntas
        float ang = 10f * Mathf.Deg2Rad;
        float yK = alto + 0.35f;
        m.Caja(Madera, new Vector3(0f, yK, 0f), new Vector3(l0, 0.45f, 0.8f));
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 c = new Vector3(s * (l0 / 2f + l1 / 2f * Mathf.Cos(ang)), yK + l1 / 2f * Mathf.Sin(ang), 0f);
            m.CajaRot(Madera, c, new Vector3(l1, 0.45f, 0.8f), Quaternion.Euler(0f, 0f, s * 10f));
        }
        return m.Construir("Torii");
    }

    // -------------------------------------------------------------- corredor

    /// <summary>Corredor techado y elevado (estilo watari-rouka) con postes, barandilla y techo largo. Va a lo largo de X.</summary>
    public static Mesh Corredor(float largo)
    {
        var m = new ConstructorMalla();
        const float ancho = 2.6f;
        const float ySuelo = 1.0f;
        const float yTopePoste = ySuelo + 3.0f;

        m.Caja(Madera, new Vector3(0f, ySuelo, 0f), new Vector3(largo, 0.15f, ancho));

        int n = Mathf.Max(2, Mathf.RoundToInt(largo / 3f));
        for (int i = 0; i <= n; i++)
        {
            float x = -largo / 2f + largo * i / n;
            for (int s = -1; s <= 1; s += 2)
            {
                float z = s * (ancho / 2f - 0.1f);
                m.Caja(Piedra, new Vector3(x, 0.1f, z), new Vector3(0.5f, 0.2f, 0.5f));
                m.Caja(Madera, new Vector3(x, (0.2f + yTopePoste) / 2f, z), new Vector3(0.22f, yTopePoste - 0.2f, 0.22f));
            }
            m.Caja(Madera, new Vector3(x, yTopePoste - 0.1f, 0f), new Vector3(0.2f, 0.2f, ancho));
        }
        for (int s = -1; s <= 1; s += 2)
        {
            float z = s * (ancho / 2f - 0.1f);
            m.Caja(Madera, new Vector3(0f, ySuelo + 0.9f, z), new Vector3(largo, 0.12f, 0.14f));
            m.Caja(Madera, new Vector3(0f, ySuelo + 0.45f, z), new Vector3(largo, 0.1f, 0.12f));
        }
        m.Techo(new Vector3(0f, yTopePoste + 0.1f, 0f), largo, ancho, 1.3f, 0.9f, 0.35f);
        return m.Construir("Corredor");
    }

    // -------------------------------------------------------------- escalera

    /// <summary>Escalera larga de piedra con muros laterales de madera. Sube hacia +X.</summary>
    public static Mesh Escalera(int peldanos, float ancho)
    {
        var m = new ConstructorMalla();
        const float alzada = 0.22f;
        const float huella = 0.45f;
        for (int i = 0; i < peldanos; i++)
        {
            float h = (i + 1) * alzada;
            m.Caja(Piedra, new Vector3(i * huella + huella / 2f, h / 2f, 0f), new Vector3(huella, h, ancho));
        }

        float runTotal = peldanos * huella;
        float riseTotal = peldanos * alzada;
        float largo = Mathf.Sqrt(runTotal * runTotal + riseTotal * riseTotal);
        float ang = Mathf.Atan2(alzada, huella) * Mathf.Rad2Deg;
        for (int s = -1; s <= 1; s += 2)
        {
            m.CajaRot(Madera, new Vector3(runTotal / 2f, riseTotal / 2f + 0.2f, s * (ancho / 2f + 0.15f)),
                      new Vector3(largo, 0.5f, 0.3f), Quaternion.Euler(0f, 0f, ang));
        }
        return m.Construir("Escalera");
    }

    // ------------------------------------------------------ linterna de piedra

    public static Mesh LinternaPiedra()
    {
        var m = new ConstructorMalla();
        AgregarLinternaPiedra(m, Vector3.zero);
        return m.Construir("LinternaPiedra");
    }

    /// <summary>Agrega una linterna de piedra (tōrō) con su luz de papel a un constructor ya existente.</summary>
    public static void AgregarLinternaPiedra(ConstructorMalla m, Vector3 pos)
    {
        Matrix4x4 guardada = m.M;
        m.M = guardada * Matrix4x4.Translate(pos);

        m.Caja(Piedra, new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.3f, 0.8f));
        m.Caja(Piedra, new Vector3(0f, 0.85f, 0f), new Vector3(0.28f, 1.1f, 0.28f));
        m.Caja(Piedra, new Vector3(0f, 1.5f, 0f), new Vector3(0.7f, 0.12f, 0.7f));
        m.Caja(Papel, new Vector3(0f, 1.78f, 0f), new Vector3(0.42f, 0.44f, 0.42f));
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
                m.Caja(Piedra, new Vector3(sx * 0.23f, 1.78f, sz * 0.23f), new Vector3(0.08f, 0.44f, 0.08f));
        }
        m.Techo(new Vector3(0f, 2.0f, 0f), 0.5f, 0.5f, 0.4f, 0.3f, 0.12f);

        m.M = guardada;
    }

    // ----------------------------------------------------------- listas hechas

    /// <summary>
    /// Las estructuras que flotan en el vacío: torres de pisos apilados (la mitad de la lista),
    /// plataformas de tatami, escaleras largas y vigas gigantes de laca roja.
    /// </summary>
    public static Mesh[] MallasDelVacio()
    {
        return new Mesh[]
        {
            TorresCastillo.Torre(8, 12f, 12f, 1),
            TorresCastillo.Torre(12, 9f, 14f, 2),
            TorresCastillo.Torre(16, 12f, 10f, 3),
            TorresCastillo.Torre(6, 18f, 14f, 4),
            TorresCastillo.Torre(10, 8f, 8f, 5),
            TorresCastillo.Torre(14, 14f, 11f, 6),
            TorresCastillo.Torre(5, 20f, 16f, 7),
            TorresCastillo.Plataforma(14f, 18f, 1),
            TorresCastillo.Plataforma(22f, 14f, 2),
            TorresCastillo.EscaleraBaranda(40, 2.4f),
            TorresCastillo.EscaleraBaranda(65, 3f),
            TorresCastillo.EscaleraBaranda(95, 3.6f),
            TorresCastillo.VigaGigante(50f),
            TorresCastillo.VigaGigante(90f)
        };
    }
}
