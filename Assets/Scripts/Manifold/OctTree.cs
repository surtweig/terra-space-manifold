using System.Collections.Generic;
using UnityEngine;

public class OctTree<TCellData, TVertexData>
   where TCellData : new()
   where TVertexData : new()
{

    public const int CellVerticesNumber = 8;
    public const int CellFacesNumber = 6;

    public enum CellVertex
    {
        NxNyNz,
        NxNyPz,
        NxPyNz,
        NxPyPz,
        PxNyNz,
        PxNyPz,
        PxPyNz,
        PxPyPz
    };

    public enum CellFace
    {
        Nx, Ny, Nz, Px, Py, Pz
    };

    public readonly Vector3Int[] CellVertex3i = new Vector3Int[]
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 1, 1),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, 1, 1)
    };

    public readonly Vector3Int[] CellFace3i = new Vector3Int[]
    {
        new Vector3Int( 1,  0,  0),
        new Vector3Int(-1,  0,  0),
        new Vector3Int( 0,  1,  0),
        new Vector3Int( 0, -1,  0),
        new Vector3Int( 0,  0,  1),
        new Vector3Int( 0,  0, -1),
    };

    public readonly CellFace[][] CellVertexDirections = new CellFace[CellVerticesNumber][]
    {
        new CellFace[3] {CellFace.Nx, CellFace.Ny, CellFace.Nz},
        new CellFace[3] {CellFace.Nx, CellFace.Ny, CellFace.Pz},
        new CellFace[3] {CellFace.Nx, CellFace.Py, CellFace.Nz},
        new CellFace[3] {CellFace.Nx, CellFace.Py, CellFace.Pz},
        new CellFace[3] {CellFace.Px, CellFace.Ny, CellFace.Nz},
        new CellFace[3] {CellFace.Px, CellFace.Ny, CellFace.Pz},
        new CellFace[3] {CellFace.Px, CellFace.Py, CellFace.Nz},
        new CellFace[3] {CellFace.Px, CellFace.Py, CellFace.Pz}
    };

    public static CellVertex Vector3iToCellVertex(Vector3Int v)
    {
        return (CellVertex)Mathf.Clamp(v.x*4 + v.y*2 + v.z, 0, 7);
    }

    public class Vertex
    {
        public TVertexData data;
        public int uid { get; private set; }
        public int baseLevel { get; private set; }
        public OctTree<TCellData, TVertexData> tree { get; private set; }
        public Vector3 position { get; private set; }

        private List<Vertex[]> connections;

        public Vertex(OctTree<TCellData, TVertexData> tree, int baseLevel, Vector3 position)
        {
            this.tree = tree;
            this.baseLevel = baseLevel;
            uid = tree.getVertexUID;
            data = new TVertexData();
            connections = new List<Vertex[]>();
            this.position = position;
            tree.vertices.Add(this);
        }

        public Vertex[] GetAdjacentVertices(int level)
        {
            if (level >= baseLevel && level < baseLevel + connections.Count)
                return connections[level - baseLevel];
            return null;
        }

        public void AddConnectionLevel(Vertex[] adjacentVertices, int level)
        {
            if (adjacentVertices.Length != CellFacesNumber)
                throw new System.ArgumentException(string.Format("There must be exactly {0} connections for vertex #{1} at level {2}.", CellFacesNumber, uid, level));

            if (level < baseLevel + connections.Count)
                throw new System.ArgumentException(string.Format("Connections for vertex #{0} at level {1} were already created.", uid, level));
            else if (level > baseLevel + connections.Count)
                throw new System.ArgumentException(string.Format("Connections for vertex #{0} at level {1} could be added only after previous level.", uid, level));
            else
            {
                connections.Add(adjacentVertices);
            }
        }
    }

    public class Cell
    {
        public TCellData data;
        public int uid { get; private set; }
        public int level { get; private set; }
        public Cell parent { get; private set; }
        public CellVertex index { get; private set; }
        public OctTree<TCellData, TVertexData> tree { get; private set; }

        private Vertex[] vertices;
        private Cell[] children;

        public Cell(OctTree<TCellData, TVertexData> tree, Vector3 position, float size)
        {
            this.tree = tree;
            level = 0;
            uid = tree.getCellUID;
            vertices = new Vertex[CellVerticesNumber];
            children = null;
            data = new TCellData();
            tree.cells.Add(this);

            //     7---4    7       Py
            //    /   /|    |       |
            //   3---0 5    6---5   6---Px
            //   |   |/    /       /
            //   2---1    2       Pz

            /*    Nx  Ny  Nz  Px  Py  Pz          
               0  3   1   4
               1  2       5       0      
               2          6   1   3      
               3      2   7   0          
               4  7   5               0  
               5  6               4   1  
               6              5   7   2  
               7      6       4       3  
            */

            vertices[0] = new Vertex(tree, level, new Vector3(size, size, size));
            vertices[1] = new Vertex(tree, level, new Vector3(size, 0f,   size));
            vertices[2] = new Vertex(tree, level, new Vector3(0f,   0f,   size));
            vertices[3] = new Vertex(tree, level, new Vector3(0f,   size, size));
            vertices[4] = new Vertex(tree, level, new Vector3(size, size, 0f));
            vertices[5] = new Vertex(tree, level, new Vector3(size, 0f,   0f));
            vertices[6] = new Vertex(tree, level, new Vector3(0f,   0f,   0f));
            vertices[7] = new Vertex(tree, level, new Vector3(0f,   size, 0f));

            //                                             Nx           Ny           Nz           Px           Py           Pz
            vertices[0].AddConnectionLevel(new Vertex[6] { vertices[3], vertices[1], vertices[4], null,        null,        null        }, level);
            vertices[1].AddConnectionLevel(new Vertex[6] { vertices[2], null,        vertices[5], null,        vertices[0], null        }, level);
            vertices[2].AddConnectionLevel(new Vertex[6] { null,        null,        vertices[6], vertices[1], vertices[3], null        }, level);
            vertices[3].AddConnectionLevel(new Vertex[6] { null,        vertices[2], vertices[7], vertices[0], null,        null        }, level);
            vertices[4].AddConnectionLevel(new Vertex[6] { vertices[7], vertices[5], null,        null,        null,        vertices[0] }, level);
            vertices[5].AddConnectionLevel(new Vertex[6] { vertices[6], null,        null,        null,        vertices[4], vertices[1] }, level);
            vertices[6].AddConnectionLevel(new Vertex[6] { null,        null,        null,        vertices[5], vertices[7], vertices[2] }, level);
            vertices[7].AddConnectionLevel(new Vertex[6] { null,        vertices[6], null,        vertices[4], null,        vertices[3] }, level);
        }

        public Cell(Cell parent, CellVertex index, Vertex[] vertices)
        {
            this.index = index;
            this.parent = parent;
            level = parent.level + 1;
            tree = parent.tree;
            uid = tree.getCellUID;
            //vertices = new Vertex[CellVerticesNumber];
            children = null;
            data = new TCellData();
            tree.cells.Add(this);
            this.vertices = vertices;
        }

        public void Procreate(int targetLevel)
        {
            if (level < targetLevel)
            {
                if (children == null)
                {
                    Vertex center = new Vertex(tree, level, (vertices[0].position + vertices[6].position)*0.5f);

                    children = new Cell[CellVerticesNumber];
                    //for (int i = 0; i < children.Length; i++)
                    //    children[i] = new Node(this, (CellVertex)i);
                }

                for (int i = 0; i < children.Length; i++)
                    children[i].Procreate(targetLevel);
            }
        }

        public Cell GetAdjacentCell(Vector3Int direction)
        {
            Vector3Int index3i = tree.CellVertex3i[(int)index];
            Vector3Int adj3i = index3i + direction;

            if ((adj3i.x == 0 || adj3i.x == 1) &&
                (adj3i.y == 0 || adj3i.y == 1) &&
                (adj3i.z == 0 || adj3i.z == 1))
            {
                return parent.children[(int)Vector3iToCellVertex(adj3i)];
            }
            else
            {
                Cell adjParent = parent.GetAdjacentCell(direction);
                if (adjParent != null)
                {
                    if (adjParent.children != null)
                    {
                        adj3i.x = (adj3i.x + 2) % 2;
                        adj3i.y = (adj3i.y + 2) % 2;
                        adj3i.z = (adj3i.z + 2) % 2;
                        return adjParent.children[(int)Vector3iToCellVertex(adj3i)];
                    }
                    else
                        return adjParent;
                }
                else
                    return null;
            }
        }
    }

    private int vertexUIDCounter = 0;
    private int getVertexUID { get { return vertexUIDCounter++; } }
    private int cellUIDCounter = 0;
    private int getCellUID { get { return cellUIDCounter++; } }

    private List<Vertex> vertices = new List<Vertex>();
    private List<Cell> cells = new List<Cell>();

    public OctTree(Vector3 minXYZ, Vector3 maxXYZ, int initialDepth)
    {

    }


}