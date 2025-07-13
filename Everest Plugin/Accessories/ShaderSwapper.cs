using UnityEngine;

namespace Everest.Accessories
{
    public class ShaderSwapper : MonoBehaviour
    {
        [SerializeField]
        private int materialIndex;

        [SerializeField]
        private string shaderName;

#if PLUGIN
        void Start()
        {
            if (shaderName == null) return;

            var renderer = GetComponent<Renderer>();

            var shader = Shader.Find(shaderName);

            if (shader)
            {
                if (renderer)
                    renderer.materials[materialIndex].shader = shader;
            }
            else
                EverestPlugin.LogWarning($"Shader with name \"{shaderName}\" not found!");
        }
#endif
    }

}
