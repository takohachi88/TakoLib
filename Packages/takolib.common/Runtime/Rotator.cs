using UnityEngine;

namespace TakoLib.Common
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Vector3 _rotation;

        private void Update()
        {
            transform.Rotate(_rotation * Time.deltaTime * 10f);
        }
    }
}