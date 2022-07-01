﻿using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HeroesPowerPlant.LevelEditor
{
    public static class BSP_IO_Shared
    {
        public static ModelConverterData ReadOBJFile(string InputFile, bool isShadowCollision)
        {
            ModelConverterData objData = new ModelConverterData()
            {
                MaterialList = new List<string>(),
                VertexList = new List<Vertex>(),
                UVList = new List<Vector2>(),
                ColorList = new List<Color>(),
                TriangleList = new List<Triangle>(),
                MTLLib = null
            };

            string[] OBJFile = File.ReadAllLines(InputFile);

            int CurrentMaterial = -1;

            bool hasUVCoords = true;

            RenderWareFile.Color TempColFlags = new RenderWareFile.Color(0, 0, 0, 0);

            foreach (string j in OBJFile)
            {
                if (j.Length > 2)
                {
                    if (j.StartsWith("v "))
                    {
                        string a = Regex.Replace(j, @"\s+", " ");

                        string[] SubStrings = a.Split(' ');
                        Vertex TempVertex = new Vertex();
                        TempVertex.Position.X = Convert.ToSingle(SubStrings[1]);
                        TempVertex.Position.Y = Convert.ToSingle(SubStrings[2]);
                        TempVertex.Position.Z = Convert.ToSingle(SubStrings[3]);

                        TempVertex.Color = Color.White;

                        objData.VertexList.Add(TempVertex);
                    }
                    else if (j.Substring(0, 3) == "vt ")
                    {
                        string[] SubStrings = j.Split(' ');
                        Vector2 TempUV = new Vector2
                        {
                            X = Convert.ToSingle(SubStrings[1]),
                            Y = Convert.ToSingle(SubStrings[2])
                        };
                        objData.UVList.Add(TempUV);
                    }
                    else if (j.Substring(0, 3) == "vc ") // Special code
                    {
                        string[] SubStrings = j.Split(' ');
                        Color TempColor = new Color
                        {
                            R = Convert.ToByte(SubStrings[1]),
                            G = Convert.ToByte(SubStrings[2]),
                            B = Convert.ToByte(SubStrings[3]),
                            A = Convert.ToByte(SubStrings[4])
                        };

                        objData.ColorList.Add(TempColor);
                    }
                    else if (j.StartsWith("f "))
                    {
                        string[] SubStrings = j.Split(' ');

                        Triangle TempTriangle = new Triangle
                        {
                            MaterialIndex = CurrentMaterial,
                            vertex1 = Convert.ToInt32(SubStrings[1].Split('/')[0]) - 1,
                            vertex2 = Convert.ToInt32(SubStrings[2].Split('/')[0]) - 1,
                            vertex3 = Convert.ToInt32(SubStrings[3].Split('/')[0]) - 1
                        };

                        if (hasUVCoords & !isShadowCollision)
                        {
                            try
                            {
                                TempTriangle.UVCoord1 = Convert.ToInt32(SubStrings[1].Split('/')[1]) - 1;
                                TempTriangle.UVCoord2 = Convert.ToInt32(SubStrings[2].Split('/')[1]) - 1;
                                TempTriangle.UVCoord3 = Convert.ToInt32(SubStrings[3].Split('/')[1]) - 1;
                            }
                            catch
                            {
                                MessageBox.Show("Error parsing texture coordinates. The model will be imported without them.");
                                hasUVCoords = false;
                                objData.UVList = new List<Vector2>() { new Vector2() };
                            }
                        }

                        objData.TriangleList.Add(TempTriangle);
                    }
                    else if (j.StartsWith("usemtl "))
                    {
                        objData.MaterialList.Add(Regex.Replace(j.Substring(7), @"\s+", ""));
                        CurrentMaterial += 1;
                    }
                    else if (j.StartsWith("mtllib "))
                        objData.MTLLib = j.Substring(7).Split('\\').LastOrDefault();
                }
            }

            if (isShadowCollision)
                return objData;

            // Special code
            if (objData.ColorList.Count == objData.VertexList.Count)
                for (int i = 0; i < objData.VertexList.Count; i++)
                {
                    Vertex v = objData.VertexList[i];
                    v.Color = objData.ColorList[i];
                    objData.VertexList[i] = v;
                }

            try
            {
                objData.MaterialList = ReplaceMaterialNames(InputFile, objData.MTLLib, objData.MaterialList);
            }
            catch
            {
                MessageBox.Show("Unable to load material lib. Will use material names as texture names.");
            }

            if (hasUVCoords)
                FixUVCoords(objData);

            return objData;
        }

        public static List<string> ReplaceMaterialNames(string InputOBJFile, string MTLLib, List<string> MaterialList)
        {
            string MTLPath = Path.Combine(Path.GetDirectoryName(InputOBJFile), MTLLib);
            string[] MTLFile = File.ReadAllLines(MTLPath);

            Dictionary<string, string> MaterialLibrary = new Dictionary<string, string>();

            string MaterialName = "";

            foreach (string j in MTLFile)
            {
                string a = Regex.Replace(j, @"\s+", "");

                if (a.StartsWith("newmtl"))
                {
                    MaterialName = a.Substring(6);
                }
                else if (a.StartsWith("map_Kd"))
                {
                    if (!MaterialLibrary.ContainsKey(MaterialName))
                        MaterialLibrary.Add(MaterialName, Path.GetFileNameWithoutExtension(a.Substring(6)));
                }
            }

            for (int k = 0; k < MaterialList.Count; k++)
            {
                if (MaterialLibrary.ContainsKey(MaterialList[k]))
                    MaterialList[k] = MaterialLibrary[MaterialList[k]];
                else
                    MessageBox.Show("Texture name for material " + MaterialList[k] + " was not found in the " + MTLPath + " file.");
            }

            return MaterialList;
        }

        public static void FixUVCoords(ModelConverterData data)
        {
            for (int i = 0; i < data.TriangleList.Count; i++)
            {
                if (data.VertexList[data.TriangleList[i].vertex1].HasUV == false)
                {
                    Vertex TempVertex = data.VertexList[data.TriangleList[i].vertex1];

                    TempVertex.TexCoord.X = data.UVList[data.TriangleList[i].UVCoord1].X;
                    TempVertex.TexCoord.Y = data.UVList[data.TriangleList[i].UVCoord1].Y;
                    TempVertex.HasUV = true;
                    data.VertexList[data.TriangleList[i].vertex1] = TempVertex;
                }
                else
                {
                    Vertex TempVertex = data.VertexList[data.TriangleList[i].vertex1];

                    if ((TempVertex.TexCoord.X != data.UVList[data.TriangleList[i].UVCoord1].X) | (TempVertex.TexCoord.Y != data.UVList[data.TriangleList[i].UVCoord1].Y))
                    {
                        TempVertex.TexCoord.X = data.UVList[data.TriangleList[i].UVCoord1].X;
                        TempVertex.TexCoord.Y = data.UVList[data.TriangleList[i].UVCoord1].Y;

                        Triangle TempTriangle = data.TriangleList[i];
                        TempTriangle.vertex1 = data.VertexList.Count;
                        data.TriangleList[i] = TempTriangle;
                        data.VertexList.Add(TempVertex);
                    }
                }
                if (data.VertexList[data.TriangleList[i].vertex2].HasUV == false)
                {
                    Vertex TempVertex = data.VertexList[data.TriangleList[i].vertex2];

                    TempVertex.TexCoord.X = data.UVList[data.TriangleList[i].UVCoord2].X;
                    TempVertex.TexCoord.Y = data.UVList[data.TriangleList[i].UVCoord2].Y;
                    TempVertex.HasUV = true;
                    data.VertexList[data.TriangleList[i].vertex2] = TempVertex;
                }
                else
                {
                    Vertex TempVertex = data.VertexList[data.TriangleList[i].vertex2];

                    if ((TempVertex.TexCoord.X != data.UVList[data.TriangleList[i].UVCoord2].X) | (TempVertex.TexCoord.Y != data.UVList[data.TriangleList[i].UVCoord2].Y))
                    {
                        TempVertex.TexCoord.X = data.UVList[data.TriangleList[i].UVCoord2].X;
                        TempVertex.TexCoord.Y = data.UVList[data.TriangleList[i].UVCoord2].Y;

                        Triangle TempTriangle = data.TriangleList[i];
                        TempTriangle.vertex2 = data.VertexList.Count;
                        data.TriangleList[i] = TempTriangle;
                        data.VertexList.Add(TempVertex);
                    }
                }
                if (data.VertexList[data.TriangleList[i].vertex3].HasUV == false)
                {
                    Vertex TempVertex = data.VertexList[data.TriangleList[i].vertex3];

                    TempVertex.TexCoord.X = data.UVList[data.TriangleList[i].UVCoord3].X;
                    TempVertex.TexCoord.Y = data.UVList[data.TriangleList[i].UVCoord3].Y;
                    TempVertex.HasUV = true;
                    data.VertexList[data.TriangleList[i].vertex3] = TempVertex;
                }
                else
                {
                    Vertex TempVertex = data.VertexList[data.TriangleList[i].vertex3];

                    if ((TempVertex.TexCoord.X != data.UVList[data.TriangleList[i].UVCoord3].X) | (TempVertex.TexCoord.Y != data.UVList[data.TriangleList[i].UVCoord3].Y))
                    {
                        TempVertex.TexCoord.X = data.UVList[data.TriangleList[i].UVCoord3].X;
                        TempVertex.TexCoord.Y = data.UVList[data.TriangleList[i].UVCoord3].Y;

                        Triangle TempTriangle = data.TriangleList[i];
                        TempTriangle.vertex3 = data.VertexList.Count;
                        data.TriangleList[i] = TempTriangle;
                        data.VertexList.Add(TempVertex);
                    }
                }
            }
        }

        public static bool ColorsAreEqual(Color left, Color right)
        {
            return
                left.R == right.R &&
                left.G == right.G &&
                left.B == right.B &&
                left.A == right.A;
        }

        public static void FixColors(ModelConverterData d)
        {
            for (int i = 0; i < d.TriangleList.Count; i++)
            {
                if (d.VertexList[d.TriangleList[i].vertex1].HasColor == false)
                {
                    Vertex TempVertex = d.VertexList[d.TriangleList[i].vertex1];

                    TempVertex.Color = d.ColorList[d.TriangleList[i].Color1];
                    TempVertex.HasColor = true;
                    d.VertexList[d.TriangleList[i].vertex1] = TempVertex;
                }
                else
                {
                    Vertex TempVertex = d.VertexList[d.TriangleList[i].vertex1];

                    if (!ColorsAreEqual(TempVertex.Color, d.ColorList[d.TriangleList[i].Color1]))
                    {
                        TempVertex.Color = d.ColorList[d.TriangleList[i].Color1];

                        Triangle TempTriangle = d.TriangleList[i];
                        TempTriangle.vertex1 = d.VertexList.Count;
                        d.TriangleList[i] = TempTriangle;
                        d.VertexList.Add(TempVertex);
                    }
                }

                if (d.VertexList[d.TriangleList[i].vertex2].HasColor == false)
                {
                    Vertex TempVertex = d.VertexList[d.TriangleList[i].vertex2];

                    TempVertex.Color = d.ColorList[d.TriangleList[i].Color2];
                    TempVertex.HasColor = true;
                    d.VertexList[d.TriangleList[i].vertex2] = TempVertex;
                }
                else
                {
                    Vertex TempVertex = d.VertexList[d.TriangleList[i].vertex2];

                    if (!ColorsAreEqual(TempVertex.Color, d.ColorList[d.TriangleList[i].Color2]))
                    {
                        TempVertex.Color = d.ColorList[d.TriangleList[i].Color2];

                        Triangle TempTriangle = d.TriangleList[i];
                        TempTriangle.vertex2 = d.VertexList.Count;
                        d.TriangleList[i] = TempTriangle;
                        d.VertexList.Add(TempVertex);
                    }
                }

                if (d.VertexList[d.TriangleList[i].vertex3].HasColor == false)
                {
                    Vertex TempVertex = d.VertexList[d.TriangleList[i].vertex3];

                    TempVertex.Color = d.ColorList[d.TriangleList[i].Color3];
                    TempVertex.HasColor = true;
                    d.VertexList[d.TriangleList[i].vertex3] = TempVertex;
                }
                else
                {
                    Vertex TempVertex = d.VertexList[d.TriangleList[i].vertex3];

                    if (!ColorsAreEqual(TempVertex.Color, d.ColorList[d.TriangleList[i].Color3]))
                    {
                        TempVertex.Color = d.ColorList[d.TriangleList[i].Color3];

                        Triangle TempTriangle = d.TriangleList[i];
                        TempTriangle.vertex3 = d.VertexList.Count;
                        d.TriangleList[i] = TempTriangle;
                        d.VertexList.Add(TempVertex);
                    }
                }
            }
        }
    }
}
