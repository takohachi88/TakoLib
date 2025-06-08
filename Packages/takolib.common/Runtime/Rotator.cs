using UnityEngine;

namespace TakoLib.Common
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Vector3 _rotation;
        [SerializeField] private Space _space = Space.World;

        private void Update()
        {
            transform.Rotate(_rotation * Time.deltaTime * 10f, _space);
        }
    }
}