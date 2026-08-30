#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace VisorDatosSIG.Infrastructure.Shapefiles;

/// <summary>
/// Generador de conjuntos de datos Shapefile (SHP, SHX, DBF, PRJ) en WGS84 para pruebas y validación del sistema.
/// </summary>
public static class ShapefileGenerator
{
    static ShapefileGenerator()
    {
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
    }

    private const string Wgs84PrjContent = "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]]";

    public static void GenerarDatasetsDePrueba(string directorioDestino)
    {
        if (!Directory.Exists(directorioDestino))
        {
            Directory.CreateDirectory(directorioDestino);
        }

        GenerarShapefilePuntos(
            Path.Combine(directorioDestino, "CodigosFijos"),
            new List<(double X, double Y, Dictionary<string, string> Atributos)>
            {
                (-77.0345, -12.0935, new Dictionary<string, string> { ["COD_FIJO"] = "CF-1001", ["TIPO_PUNTO"] = "Vértice Geodésico", ["DESC"] = "Punto IGN-01", ["COTA_Z"] = "108.45", ["REF"] = "Cruce Av. Libertadores" }),
                (-77.0308, -12.0938, new Dictionary<string, string> { ["COD_FIJO"] = "CF-1002", ["TIPO_PUNTO"] = "Hito Catastral", ["DESC"] = "Hito Límite", ["COTA_Z"] = "109.12", ["REF"] = "Noreste MZ-B" }),
                (-77.0375, -12.0945, new Dictionary<string, string> { ["COD_FIJO"] = "CF-1003", ["TIPO_PUNTO"] = "Poste", ["DESC"] = "Poste MT N° 5542", ["COTA_Z"] = "107.89", ["REF"] = "Frente LT-A-01" }),
                (-77.0348, -12.0975, new Dictionary<string, string> { ["COD_FIJO"] = "CF-1004", ["TIPO_PUNTO"] = "Cámara", ["DESC"] = "Buzón Matriz SEDAPAL", ["COTA_Z"] = "106.32", ["REF"] = "Calle Los Álamos" }),
                (-77.0382, -12.1012, new Dictionary<string, string> { ["COD_FIJO"] = "CF-1005", ["TIPO_PUNTO"] = "Hito Catastral", ["DESC"] = "Vértice Esquina MZ-C", ["COTA_Z"] = "105.70", ["REF"] = "Las Flores" }),
                (-77.0320, -12.0965, new Dictionary<string, string> { ["COD_FIJO"] = "CF-1006", ["TIPO_PUNTO"] = "Nivelación", ["DESC"] = "Placa Bronce BM-24", ["COTA_Z"] = "108.65", ["REF"] = "Centro Médico" }),
                (-77.0335, -12.0988, new Dictionary<string, string> { ["COD_FIJO"] = "CF-1007", ["TIPO_PUNTO"] = "Medidor", ["DESC"] = "Válvula VRP-03", ["COTA_Z"] = "106.15", ["REF"] = "Av. Libertadores Cdra 2" }),
                (-77.0315, -12.1005, new Dictionary<string, string> { ["COD_FIJO"] = "CF-1008", ["TIPO_PUNTO"] = "Control", ["DESC"] = "Estación EMA-01", ["COTA_Z"] = "105.40", ["REF"] = "Parque Infantil" })
            },
            new List<(string Nombre, char Tipo, byte Longitud)>
            {
                ("COD_FIJO", 'C', 20),
                ("TIPO_PUNTO", 'C', 30),
                ("DESC", 'C', 50),
                ("COTA_Z", 'C', 15),
                ("REF", 'C', 50)
            });

        GenerarShapefileLineas(
            Path.Combine(directorioDestino, "Vias"),
            new List<(List<(double X, double Y)> Puntos, Dictionary<string, string> Atributos)>
            {
                (new List<(double X, double Y)> { (-77.0390, -12.0935), (-77.0345, -12.0935), (-77.0300, -12.0935) },
                 new Dictionary<string, string> { ["COD_VIA"] = "VIA-01", ["NOM_VIA"] = "Los Libertadores", ["TIPO_VIA"] = "Avenida", ["JERARQUIA"] = "Arteria Principal", ["SENTIDO"] = "Doble Sentido", ["ANCHO_VIA"] = "24.00", ["PAVIMENTO"] = "Asfalto en Caliente" }),
                (new List<(double X, double Y)> { (-77.0345, -12.0930), (-77.0345, -12.0975), (-77.0345, -12.1015) },
                 new Dictionary<string, string> { ["COD_VIA"] = "VIA-02", ["NOM_VIA"] = "El Sol", ["TIPO_VIA"] = "Avenida", ["JERARQUIA"] = "Arteria Principal", ["SENTIDO"] = "Doble Sentido", ["ANCHO_VIA"] = "22.00", ["PAVIMENTO"] = "Asfalto en Caliente" }),
                (new List<(double X, double Y)> { (-77.0390, -12.0975), (-77.0345, -12.0975), (-77.0300, -12.0975) },
                 new Dictionary<string, string> { ["COD_VIA"] = "VIA-03", ["NOM_VIA"] = "Los Álamos", ["TIPO_VIA"] = "Calle", ["JERARQUIA"] = "Colectora", ["SENTIDO"] = "Un Sentido", ["ANCHO_VIA"] = "14.00", ["PAVIMENTO"] = "Pavimento Rígido" }),
                (new List<(double X, double Y)> { (-77.0305, -12.0935), (-77.0305, -12.0975), (-77.0305, -12.1015) },
                 new Dictionary<string, string> { ["COD_VIA"] = "VIA-04", ["NOM_VIA"] = "Los Pinos", ["TIPO_VIA"] = "Calle", ["JERARQUIA"] = "Local", ["SENTIDO"] = "Un Sentido", ["ANCHO_VIA"] = "12.00", ["PAVIMENTO"] = "Adoquín Concreto" }),
                (new List<(double X, double Y)> { (-77.0385, -12.0935), (-77.0385, -12.0975), (-77.0385, -12.1015) },
                 new Dictionary<string, string> { ["COD_VIA"] = "VIA-05", ["NOM_VIA"] = "Santa Rosa", ["TIPO_VIA"] = "Pasaje", ["JERARQUIA"] = "Peatonal", ["SENTIDO"] = "Peatonal", ["ANCHO_VIA"] = "6.00", ["PAVIMENTO"] = "Adoquín Decorativo" }),
                (new List<(double X, double Y)> { (-77.0390, -12.1015), (-77.0345, -12.1015), (-77.0300, -12.1015) },
                 new Dictionary<string, string> { ["COD_VIA"] = "VIA-06", ["NOM_VIA"] = "Las Palmeras", ["TIPO_VIA"] = "Jirón", ["JERARQUIA"] = "Local", ["SENTIDO"] = "Doble Sentido", ["ANCHO_VIA"] = "15.00", ["PAVIMENTO"] = "Asfalto en Caliente" })
            },
            new List<(string Nombre, char Tipo, byte Longitud)>
            {
                ("COD_VIA", 'C', 20),
                ("NOM_VIA", 'C', 50),
                ("TIPO_VIA", 'C', 20),
                ("JERARQUIA", 'C', 30),
                ("SENTIDO", 'C', 20),
                ("ANCHO_VIA", 'C', 15),
                ("PAVIMENTO", 'C', 30)
            });

        GenerarShapefilePoligonos(
            Path.Combine(directorioDestino, "Manzanas"),
            new List<(List<(double X, double Y)> Puntos, Dictionary<string, string> Atributos)>
            {
                (new List<(double X, double Y)> { (-77.0380, -12.0940), (-77.0350, -12.0940), (-77.0350, -12.0970), (-77.0380, -12.0970), (-77.0380, -12.0940) },
                 new Dictionary<string, string> { ["COD_MZ"] = "MZ-A", ["SECTOR"] = "Sector 01", ["BARRIO"] = "San Isidro Centro", ["AREA_M2"] = "12500.50", ["PERIM"] = "450.20" }),
                (new List<(double X, double Y)> { (-77.0340, -12.0940), (-77.0310, -12.0940), (-77.0310, -12.0970), (-77.0340, -12.0970), (-77.0340, -12.0940) },
                 new Dictionary<string, string> { ["COD_MZ"] = "MZ-B", ["SECTOR"] = "Sector 01", ["BARRIO"] = "San Isidro Centro", ["AREA_M2"] = "13200.00", ["PERIM"] = "465.80" }),
                (new List<(double X, double Y)> { (-77.0380, -12.0980), (-77.0350, -12.0980), (-77.0350, -12.1010), (-77.0380, -12.1010), (-77.0380, -12.0980) },
                 new Dictionary<string, string> { ["COD_MZ"] = "MZ-C", ["SECTOR"] = "Sector 02", ["BARRIO"] = "Las Flores", ["AREA_M2"] = "11800.75", ["PERIM"] = "438.40" }),
                (new List<(double X, double Y)> { (-77.0340, -12.0980), (-77.0310, -12.0980), (-77.0310, -12.1010), (-77.0340, -12.1010), (-77.0340, -12.0980) },
                 new Dictionary<string, string> { ["COD_MZ"] = "MZ-D", ["SECTOR"] = "Sector 02", ["BARRIO"] = "Las Flores", ["AREA_M2"] = "12950.20", ["PERIM"] = "458.10" })
            },
            new List<(string Nombre, char Tipo, byte Longitud)>
            {
                ("COD_MZ", 'C', 20),
                ("SECTOR", 'C', 30),
                ("BARRIO", 'C', 50),
                ("AREA_M2", 'C', 18),
                ("PERIM", 'C', 18)
            });

        GenerarShapefilePoligonos(
            Path.Combine(directorioDestino, "Lotes"),
            new List<(List<(double X, double Y)> Puntos, Dictionary<string, string> Atributos)>
            {
                (new List<(double X, double Y)> { (-77.0380, -12.0940), (-77.0365, -12.0940), (-77.0365, -12.0955), (-77.0380, -12.0955), (-77.0380, -12.0940) },
                 new Dictionary<string, string> { ["COD_LOTE"] = "LT-A-01", ["COD_MZ"] = "MZ-A", ["NUM_PRED"] = "P-101", ["USO_SUELO"] = "Residencial R3", ["TITULAR"] = "Carlos Mendoza Ramos", ["DIR"] = "Av. Los Libertadores 142", ["AREA"] = "4100.00", ["FRENTE"] = "15.00", ["FONDO"] = "25.00" }),
                (new List<(double X, double Y)> { (-77.0365, -12.0940), (-77.0350, -12.0940), (-77.0350, -12.0955), (-77.0365, -12.0955), (-77.0365, -12.0940) },
                 new Dictionary<string, string> { ["COD_LOTE"] = "LT-A-02", ["COD_MZ"] = "MZ-A", ["NUM_PRED"] = "P-102", ["USO_SUELO"] = "Comercial C2", ["TITULAR"] = "Inmobiliaria El Roble S.A.C.", ["DIR"] = "Av. Los Libertadores 156", ["AREA"] = "4100.00", ["FRENTE"] = "15.00", ["FONDO"] = "25.00" }),
                (new List<(double X, double Y)> { (-77.0380, -12.0955), (-77.0350, -12.0955), (-77.0350, -12.0970), (-77.0380, -12.0970), (-77.0380, -12.0955) },
                 new Dictionary<string, string> { ["COD_LOTE"] = "LT-A-03", ["COD_MZ"] = "MZ-A", ["NUM_PRED"] = "P-103", ["USO_SUELO"] = "Residencial R3", ["TITULAR"] = "Elena Gutiérrez Vega", ["DIR"] = "Calle Los Álamos 210", ["AREA"] = "4300.50", ["FRENTE"] = "20.00", ["FONDO"] = "25.00" }),
                (new List<(double X, double Y)> { (-77.0340, -12.0940), (-77.0325, -12.0940), (-77.0325, -12.0955), (-77.0340, -12.0955), (-77.0340, -12.0940) },
                 new Dictionary<string, string> { ["COD_LOTE"] = "LT-B-01", ["COD_MZ"] = "MZ-B", ["NUM_PRED"] = "P-201", ["USO_SUELO"] = "Comercial C3", ["TITULAR"] = "Banco Metropolitano", ["DIR"] = "Av. El Sol 402", ["AREA"] = "4400.00", ["FRENTE"] = "18.00", ["FONDO"] = "28.00" }),
                (new List<(double X, double Y)> { (-77.0325, -12.0940), (-77.0310, -12.0940), (-77.0310, -12.0955), (-77.0325, -12.0955), (-77.0325, -12.0940) },
                 new Dictionary<string, string> { ["COD_LOTE"] = "LT-B-02", ["COD_MZ"] = "MZ-B", ["NUM_PRED"] = "P-202", ["USO_SUELO"] = "Residencial R4", ["TITULAR"] = "Lucía Fernández Silva", ["DIR"] = "Av. El Sol 418", ["AREA"] = "4400.00", ["FRENTE"] = "18.00", ["FONDO"] = "28.00" }),
                (new List<(double X, double Y)> { (-77.0340, -12.0955), (-77.0310, -12.0955), (-77.0310, -12.0970), (-77.0340, -12.0970), (-77.0340, -12.0955) },
                 new Dictionary<string, string> { ["COD_LOTE"] = "LT-B-03", ["COD_MZ"] = "MZ-B", ["NUM_PRED"] = "P-203", ["USO_SUELO"] = "Equipamiento", ["TITULAR"] = "Centro Médico San Isidro", ["DIR"] = "Calle Los Pinos 305", ["AREA"] = "4400.00", ["FRENTE"] = "22.00", ["FONDO"] = "30.00" })
            },
            new List<(string Nombre, char Tipo, byte Longitud)>
            {
                ("COD_LOTE", 'C', 20),
                ("COD_MZ", 'C', 20),
                ("NUM_PRED", 'C', 20),
                ("USO_SUELO", 'C', 30),
                ("TITULAR", 'C', 60),
                ("DIR", 'C', 60),
                ("AREA", 'C', 18),
                ("FRENTE", 'C', 15),
                ("FONDO", 'C', 15)
            });
    }

    private static void GenerarShapefilePuntos(string baseRuta, List<(double X, double Y, Dictionary<string, string> Atributos)> registros, List<(string Nombre, char Tipo, byte Longitud)> campos)
    {
        File.WriteAllText(baseRuta + ".prj", Wgs84PrjContent);
        GenerarDbf(baseRuta + ".dbf", campos, registros.Select(r => r.Atributos).ToList());

        using var fsShp = new FileStream(baseRuta + ".shp", FileMode.Create);
        using var bwShp = new BinaryWriter(fsShp);

        using var fsShx = new FileStream(baseRuta + ".shx", FileMode.Create);
        using var bwShx = new BinaryWriter(fsShx);

        double minX = registros.Min(r => r.X);
        double minY = registros.Min(r => r.Y);
        double maxX = registros.Max(r => r.X);
        double maxY = registros.Max(r => r.Y);

        int recordLengthBytes = 28; // 8 header + 4 type + 16 (X,Y)
        int totalShpBytes = 100 + (registros.Count * recordLengthBytes);
        int totalShxBytes = 100 + (registros.Count * 8);

        EscribirCabecera(bwShp, totalShpBytes / 2, 1, minX, minY, maxX, maxY);
        EscribirCabecera(bwShx, totalShxBytes / 2, 1, minX, minY, maxX, maxY);

        int offsetWords = 50;
        for (int i = 0; i < registros.Count; i++)
        {
            var r = registros[i];

            // SHX entry
            EscribirEnteroBigEndian(bwShx, offsetWords);
            EscribirEnteroBigEndian(bwShx, 10); // 20 bytes / 2

            // SHP record
            EscribirEnteroBigEndian(bwShp, i + 1);
            EscribirEnteroBigEndian(bwShp, 10); // 20 bytes content = 10 words
            bwShp.Write((int)1); // ShapeType Point
            bwShp.Write(r.X);
            bwShp.Write(r.Y);

            offsetWords += recordLengthBytes / 2;
        }
    }

    private static void GenerarShapefileLineas(string baseRuta, List<(List<(double X, double Y)> Puntos, Dictionary<string, string> Atributos)> registros, List<(string Nombre, char Tipo, byte Longitud)> campos)
    {
        File.WriteAllText(baseRuta + ".prj", Wgs84PrjContent);
        GenerarDbf(baseRuta + ".dbf", campos, registros.Select(r => r.Atributos).ToList());

        using var fsShp = new FileStream(baseRuta + ".shp", FileMode.Create);
        using var bwShp = new BinaryWriter(fsShp);

        using var fsShx = new FileStream(baseRuta + ".shx", FileMode.Create);
        using var bwShx = new BinaryWriter(fsShx);

        double minX = registros.SelectMany(r => r.Puntos).Min(p => p.X);
        double minY = registros.SelectMany(r => r.Puntos).Min(p => p.Y);
        double maxX = registros.SelectMany(r => r.Puntos).Max(p => p.X);
        double maxY = registros.SelectMany(r => r.Puntos).Max(p => p.Y);

        EscribirCabecera(bwShp, 0, 3, minX, minY, maxX, maxY);
        EscribirCabecera(bwShx, 0, 3, minX, minY, maxX, maxY);

        int offsetWords = 50;
        for (int i = 0; i < registros.Count; i++)
        {
            var r = registros[i];
            int numPoints = r.Puntos.Count;
            int contentBytes = 48 + (numPoints * 16); // 4 type + 32 box + 4 numParts + 4 numPoints + 4 part0 + (pts * 16)
            int contentWords = contentBytes / 2;

            EscribirEnteroBigEndian(bwShx, offsetWords);
            EscribirEnteroBigEndian(bwShx, contentWords);

            EscribirEnteroBigEndian(bwShp, i + 1);
            EscribirEnteroBigEndian(bwShp, contentWords);

            bwShp.Write((int)3); // PolyLine
            bwShp.Write(r.Puntos.Min(p => p.X));
            bwShp.Write(r.Puntos.Min(p => p.Y));
            bwShp.Write(r.Puntos.Max(p => p.X));
            bwShp.Write(r.Puntos.Max(p => p.Y));

            bwShp.Write((int)1); // NumParts
            bwShp.Write(numPoints); // NumPoints
            bwShp.Write((int)0); // Part 0 index

            foreach (var p in r.Puntos)
            {
                bwShp.Write(p.X);
                bwShp.Write(p.Y);
            }

            offsetWords += (8 + contentBytes) / 2;
        }

        // Actualizar longitud final en cabecera
        int totalShpWords = (int)(fsShp.Length / 2);
        int totalShxWords = (int)(fsShx.Length / 2);
        fsShp.Seek(24, SeekOrigin.Begin);
        EscribirEnteroBigEndian(bwShp, totalShpWords);
        fsShx.Seek(24, SeekOrigin.Begin);
        EscribirEnteroBigEndian(bwShx, totalShxWords);
    }

    private static void GenerarShapefilePoligonos(string baseRuta, List<(List<(double X, double Y)> Puntos, Dictionary<string, string> Atributos)> registros, List<(string Nombre, char Tipo, byte Longitud)> campos)
    {
        File.WriteAllText(baseRuta + ".prj", Wgs84PrjContent);
        GenerarDbf(baseRuta + ".dbf", campos, registros.Select(r => r.Atributos).ToList());

        using var fsShp = new FileStream(baseRuta + ".shp", FileMode.Create);
        using var bwShp = new BinaryWriter(fsShp);

        using var fsShx = new FileStream(baseRuta + ".shx", FileMode.Create);
        using var bwShx = new BinaryWriter(fsShx);

        double minX = registros.SelectMany(r => r.Puntos).Min(p => p.X);
        double minY = registros.SelectMany(r => r.Puntos).Min(p => p.Y);
        double maxX = registros.SelectMany(r => r.Puntos).Max(p => p.X);
        double maxY = registros.SelectMany(r => r.Puntos).Max(p => p.Y);

        EscribirCabecera(bwShp, 0, 5, minX, minY, maxX, maxY);
        EscribirCabecera(bwShx, 0, 5, minX, minY, maxX, maxY);

        int offsetWords = 50;
        for (int i = 0; i < registros.Count; i++)
        {
            var r = registros[i];
            int numPoints = r.Puntos.Count;
            int contentBytes = 48 + (numPoints * 16);
            int contentWords = contentBytes / 2;

            EscribirEnteroBigEndian(bwShx, offsetWords);
            EscribirEnteroBigEndian(bwShx, contentWords);

            EscribirEnteroBigEndian(bwShp, i + 1);
            EscribirEnteroBigEndian(bwShp, contentWords);

            bwShp.Write((int)5); // Polygon
            bwShp.Write(r.Puntos.Min(p => p.X));
            bwShp.Write(r.Puntos.Min(p => p.Y));
            bwShp.Write(r.Puntos.Max(p => p.X));
            bwShp.Write(r.Puntos.Max(p => p.Y));

            bwShp.Write((int)1); // NumParts
            bwShp.Write(numPoints); // NumPoints
            bwShp.Write((int)0); // Part 0

            foreach (var p in r.Puntos)
            {
                bwShp.Write(p.X);
                bwShp.Write(p.Y);
            }

            offsetWords += (8 + contentBytes) / 2;
        }

        int totalShpWords = (int)(fsShp.Length / 2);
        int totalShxWords = (int)(fsShx.Length / 2);
        fsShp.Seek(24, SeekOrigin.Begin);
        EscribirEnteroBigEndian(bwShp, totalShpWords);
        fsShx.Seek(24, SeekOrigin.Begin);
        EscribirEnteroBigEndian(bwShx, totalShxWords);
    }

    private static void GenerarDbf(string dbfPath, List<(string Nombre, char Tipo, byte Longitud)> campos, List<Dictionary<string, string>> registros)
    {
        using var fs = new FileStream(dbfPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        short headerLength = (short)(32 + (campos.Count * 32) + 1);
        short recordLength = (short)(1 + campos.Sum(c => c.Longitud));

        // Cabecera dBASE III
        bw.Write((byte)0x03); // Version
        bw.Write((byte)(DateTime.UtcNow.Year - 1900));
        bw.Write((byte)DateTime.UtcNow.Month);
        bw.Write((byte)DateTime.UtcNow.Day);
        bw.Write(registros.Count);
        bw.Write(headerLength);
        bw.Write(recordLength);
        bw.Write(new byte[20]); // Reservado

        // Descriptores de campo
        foreach (var c in campos)
        {
            byte[] nameBytes = new byte[11];
            var ascii = Encoding.ASCII.GetBytes(c.Nombre);
            Array.Copy(ascii, nameBytes, Math.Min(ascii.Length, 10));
            bw.Write(nameBytes);
            bw.Write((byte)c.Tipo);
            bw.Write((int)0); // Offset reservado
            bw.Write(c.Longitud);
            bw.Write((byte)0); // Decimales
            bw.Write(new byte[14]); // Reservado
        }

        bw.Write((byte)0x0D); // Terminador cabecera

        // Registros
        foreach (var reg in registros)
        {
            bw.Write((byte)0x20); // No borrado
            foreach (var c in campos)
            {
                reg.TryGetValue(c.Nombre, out var val);
                val ??= "";

                byte[] raw = new byte[c.Longitud];
                for (int b = 0; b < raw.Length; b++) raw[b] = 0x20; // Rellenar con espacios

                var valBytes = Encoding.GetEncoding(1252).GetBytes(val);
                Array.Copy(valBytes, raw, Math.Min(valBytes.Length, c.Longitud));
                bw.Write(raw);
            }
        }

        bw.Write((byte)0x1A); // EOF
    }

    private static void EscribirCabecera(BinaryWriter bw, int fileLengthWords, int shapeType, double minX, double minY, double maxX, double maxY)
    {
        EscribirEnteroBigEndian(bw, 9994); // FileCode
        for (int i = 0; i < 5; i++) EscribirEnteroBigEndian(bw, 0); // Unused
        EscribirEnteroBigEndian(bw, fileLengthWords); // File Length

        bw.Write((int)1000); // Version
        bw.Write(shapeType); // ShapeType
        bw.Write(minX);
        bw.Write(minY);
        bw.Write(maxX);
        bw.Write(maxY);
        bw.Write((double)0.0); // Zmin
        bw.Write((double)0.0); // Zmax
        bw.Write((double)0.0); // Mmin
        bw.Write((double)0.0); // Mmax
    }

    private static void EscribirEnteroBigEndian(BinaryWriter bw, int valor)
    {
        byte[] bytes = BitConverter.GetBytes(valor);
        Array.Reverse(bytes);
        bw.Write(bytes);
    }
}
