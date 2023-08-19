using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[HelpURL("https://discussions.unity.com/t/load-texture-from-gpu/210626")]
[AddComponentMenu("V037/Water Component")]
public class monocularcringe : MonoBehaviour
{   
    [Header ("--Components--")]
    [SerializeField] private ComputeShader shader;
    [SerializeField] private Material material;
    [SerializeField] private Transform water; 
    [SerializeField] private RenderTexture rend; 

    [Header ("--Conditions--")]
    [SerializeField] private bool startShader;
    [SerializeField] private bool renderShader; 
    [SerializeField] private bool debuhg_mode;
    [SerializeField] private bool debuhg_show;

    [Header ("--Values--")]
    [Range (0.0f, 1.0f)] [SerializeField] private float waterLevel;
    [SerializeField] private int forV;
    [SerializeField] private int computeThreads;
    [SerializeField] private float particlesScale = 0.2f;
    [SerializeField] private uint debuhg_population = 8;

    [Header ("--Debug--")]
    [SerializeField] private Vector3[] Dparticles = new Vector3[1];
    [SerializeField] private Vector3[] DparticlesVelocity = new Vector3[1];
    [SerializeField] private Vector3[] DparticlesTemp = new Vector3[1];
    [SerializeField] private uint[] DparticlesCounts = new uint[1];

    private Vector2Int gridSize;
    private Vector3[] particles = new Vector3[1];
    private Vector3[] particlesVelocity = new Vector3[1];
    private Vector3[] particlesTemp = new Vector3[1];
    private uint[] particlesCounts = new uint[1];
    
    private int population = 0;
    private int debuhg_ThreadGroups;

    private int kernelHandle;
    private int kernelHandle1;
    private int kernelHandle2;
    
    private ComputeBuffer buff;     //buffer particle physics
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    private Mesh mesh;
    private Bounds bounds;

    private struct ParticleStruct
    {
        public Vector3 pos;
        public Vector3 vel;
        public Vector3 temp;
        public uint counts;
    }

    private struct MeshProperties
    {
        public Vector4 color;
    }

    private void InitializeBuffers()
    {
        kernelHandle = shader.FindKernel("CSMain");
        kernelHandle1 = shader.FindKernel("Colllisions");
        kernelHandle2 = shader.FindKernel("Gridding");

        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)population;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        //(int tot elemnti, int grandezza ogni singolo elemento)
        buff = new ComputeBuffer(population, 4*3*3+4);
        meshPropertiesBuffer = new ComputeBuffer(population, 4*4);
        
        ParticleStruct[] data = new ParticleStruct[population];
        MeshProperties[] properties = new MeshProperties[population];
        
        for(int x = 0; x<forV; x++)
        {
            for(int y = 0; y<forV*waterLevel; y++)
            {
                for(int z = 0; z<forV; z++)
                {
                    particles[(Mathf.CeilToInt(x*forV*forV*waterLevel))+(y*forV)+z] = new Vector3(x*0.5f,y*0.5f,z*0.5f);
                    properties[(Mathf.CeilToInt(x*forV*forV*waterLevel))+(y*forV)+z].color = Color.Lerp(Color.yellow, Color.red, Random.value); //gree, blue
                }
            }
        }

        for(int i=0; i < particles.Length; i++)
        {
            data[i].pos = particles[i];
            data[i].vel = particlesVelocity[i];
            data[i].temp = particlesTemp[i];
            data[i].counts = particlesCounts[i];   //check if possible to remove
        }

        buff.SetData(data);
        meshPropertiesBuffer.SetData(properties);
                
        shader.SetBuffer(kernelHandle, "_dataBuffer", buff); //buffer del compute shader
        
        shader.SetBuffer(kernelHandle1, "_dataBuffer", buff);   //n. del kernel, nome buffer, computebuffer con la sua dimensione preimpostata
        shader.SetTexture(kernelHandle1, "_grid", rend);
        
        shader.SetBuffer(kernelHandle2, "_dataBuffer", buff);
        shader.SetTexture(kernelHandle2, "_grid", rend);

        material.SetBuffer("_dataBuffer", buff);             //buffer per la vertex shader
        material.SetBuffer("_Properties", meshPropertiesBuffer);
        //shader.SetBuffer(kernelHandle2, "_Properties", meshPropertiesBuffer); //per mandare il colore nel compute shader
    }

    private void RunShader()
    {

        //Debug.Log(buff1.stride);      //stride è la grandezza in byte di tutti gli elemtenti penso
        //Debug.Log(data.Length/32);
        shader.Dispatch(kernelHandle2, Mathf.CeilToInt(population/computeThreads), 1, 1);
        
        shader.Dispatch(kernelHandle, Mathf.CeilToInt(population/computeThreads), 1, 1);
        
        shader.Dispatch(kernelHandle1, Mathf.CeilToInt(population/computeThreads), 1, 1);
        

        if(debuhg_mode)
        {
            if(population <= 512)
            {
                ParticleStruct[] output = new ParticleStruct[particles.Length];
                buff.GetData(output); //copiaggio dei dati nel nuovo array
                for(int i=0; i < debuhg_population; i++)
                {
                    Dparticles[i] = output[i].pos;
                    DparticlesVelocity[i] = output[i].vel;
                    DparticlesTemp[i] = output[i].temp;
                    DparticlesCounts[i] = output[i].counts;
                }
                
            }else
            {
                Debug.Log("can't debug because over population of objects");
            }
        }
    }

    private void Awake()
    {
        //p = !(kernelHandle<0.0);
        population = Mathf.CeilToInt(forV*forV*forV*waterLevel);
        particles = new Vector3[population];
        particlesVelocity = new Vector3[population];
        particlesTemp = new Vector3[population];
        particlesCounts = new uint[population];

        if(debuhg_mode)
        {
            Dparticles = new Vector3[debuhg_population];
            DparticlesVelocity = new Vector3[debuhg_population];
            DparticlesTemp = new Vector3[debuhg_population];
            DparticlesCounts = new uint[debuhg_population];
        }

        gridSize = new Vector2Int(population,Mathf.CeilToInt(population*0.25f));
        rend = new RenderTexture(gridSize.x, gridSize.y, 0, RenderTextureFormat.RInt);
        rend.enableRandomWrite = true;
        rend.Create();

        Mesh mesh = CreateQuad();
        this.mesh = mesh;

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(transform.position, Vector3.one * (population*particlesScale+10));

        debuhg_ThreadGroups = population / computeThreads;
    }

    private void Start()
    {
        InitializeBuffers();
    }

    private void Update() 
    {
        // We used to just be able to use `population` here, but it looks like a Unity update imposed a thread limit (65535) on my device.
        if(renderShader)
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
        }
    }

    void FixedUpdate()
    {   
        if(startShader)
        {
            RunShader();
            material.SetVector("_waterPosition",water.position);
        }
        //buff.Dispose();
    }

    private Mesh CreateQuad() 
    {
        var mesh = new Mesh();
        var vertices = new Vector3[6]
        {
            new Vector3(0,-1.4f*particlesScale,0),     //sotto
            new Vector3(1*particlesScale,0,1*particlesScale),         //mezzo n.1
            new Vector3(1*particlesScale,0,-1*particlesScale),        //mezzo n.2
            new Vector3(-1*particlesScale,0,1*particlesScale),        //mezzo n.3
            new Vector3(-1*particlesScale,0,-1*particlesScale),       //mezzo n.4
            new Vector3(0,1.4f*particlesScale,0)       //sopra
        };

        var tris = new int[24]
        {
            5,1,2,
            5,3,1,
            5,4,3,
            5,2,4,
            0,2,1,
            0,1,3,
            0,3,4,
            0,4,2            
        };

        var normals = new Vector3[6]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[6]
        {
            new Vector2(0,-1.4f*particlesScale),     //sotto
            new Vector2(1*particlesScale,1*particlesScale),         //mezzo n.1
            new Vector2(1*particlesScale,-1*particlesScale),        //mezzo n.2
            new Vector2(-1*particlesScale,1*particlesScale),        //mezzo n.3
            new Vector2(-1*particlesScale,-1*particlesScale),       //mezzo n.4
            new Vector2(0,1.4f*particlesScale)       //sopra
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    private void OnDisable()    //evitiamo i leak dalla memoria
    {
        if(meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if(argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
        
        if(buff != null)
        {
            buff.Release();
        }
        buff = null;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(monocularcringe))]
    public class MyScriptEditor: Editor
    {
        public override void OnInspectorGUI() 
        {
            // Call normal GUI (displaying "a" and any other variables you might have)
            base.OnInspectorGUI();

            // Reference the variables in the script
            monocularcringe script = (monocularcringe)target;

            if (script.debuhg_show) 
            {
                // Ensure the label and the value are on the same line
                EditorGUILayout.BeginHorizontal();

                // A label that says "b" (change b to B if you want it uppercase like default) and restrict its length.
                // You can change 50 to any other value
                EditorGUILayout.LabelField("Thread Groups:", GUILayout.MaxWidth(100));
                EditorGUILayout.LabelField(""+script.debuhg_ThreadGroups,GUILayout.MaxWidth(100));
                // Show and save the value of b
                EditorGUILayout.LabelField("",GUILayout.MaxWidth(10));
                EditorGUILayout.LabelField("Kernels index",GUILayout.MaxWidth(100));

                EditorGUILayout.LabelField("CsM: "+script.kernelHandle,GUILayout.MaxWidth(50));
                EditorGUILayout.LabelField("Col: "+script.kernelHandle1,GUILayout.MaxWidth(50));
                EditorGUILayout.LabelField("Gri: "+script.kernelHandle2,GUILayout.MaxWidth(50));
                
                //script.kernelHandle2 = EditorGUILayout.IntField(script.kernelHandle2, GUILayout.MaxWidth(50));

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Population:", GUILayout.MaxWidth(70));
                EditorGUILayout.LabelField(""+script.population,GUILayout.MaxWidth(142));
                EditorGUILayout.LabelField("Grid Size", GUILayout.MaxWidth(100));
                EditorGUILayout.LabelField("x: "+script.gridSize.x,GUILayout.MaxWidth(100));
                EditorGUILayout.LabelField("y: "+script.gridSize.y,GUILayout.MaxWidth(100));

                EditorGUILayout.EndHorizontal();
            }
        }
    }
#endif
}