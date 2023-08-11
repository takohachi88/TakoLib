using UnityEngine;

namespace Tako.Common.Extensions
{
    public static class TransformExtensions
    {
        #region Tranaform position and localPosition

        public static Transform SetPosition(this Transform self, Vector3 position)
        {
            self.position = position;
            return self;
        }

        public static Transform SetPositionX(this Transform self, float x)
        {
            var position = self.position;
            position.x = x;
            self.position = position;
            return self;
        }

        public static Transform SetPositionY(this Transform self, float y)
        {
            var position = self.position;
            position.y = y;
            self.position = position;
            return self;
        }

        public static Transform SetPositionZ(this Transform self, float z)
        {
            var position = self.position;
            position.z = z;
            self.position = position;
            return self;
        }

        public static Transform SetLocalPosition(this Transform self, Vector3 localPosition)
        {
            self.localPosition = localPosition;
            return self;
        }

        public static Transform SetLocalPositionX(this Transform self, float x)
        {
            var localPosition = self.localPosition;
            localPosition.x = x;
            self.localPosition = localPosition;
            return self;
        }

        public static Transform SetLocalPositionY(this Transform self, float y)
        {
            var localPosition = self.localPosition;
            localPosition.y = y;
            self.localPosition = localPosition;
            return self;
        }

        public static Transform SetLocalPositionZ(this Transform self, float z)
        {
            var localPosition = self.localPosition;
            localPosition.z = z;
            self.localPosition = localPosition;
            return self;
        }

        #endregion

        #region Transform rotation and localRotation

        public static Transform SetRotation(this Transform self, Vector3 rotation)
        {
            self.rotation = Quaternion.Euler(rotation);
            return self;
        }

        public static Transform SetRotationX(this Transform self, float x)
        {
            var rotation = self.rotation.eulerAngles;
            rotation.x = x;
            self.rotation = Quaternion.Euler(rotation);
            return self;
        }

        public static Transform SetRotationY(this Transform self, float y)
        {
            var rotation = self.rotation.eulerAngles;
            rotation.y = y;
            self.rotation = Quaternion.Euler(rotation);
            return self;
        }

        public static Transform SetRotationZ(this Transform self, float z)
        {
            var rotation = self.rotation.eulerAngles;
            rotation.z = z;
            self.rotation = Quaternion.Euler(rotation);
            return self;
        }

        public static Transform SetLocalRotation(this Transform self, Vector3 localRotation)
        {
            self.localRotation = Quaternion.Euler(localRotation);
            return self;
        }

        public static Transform SetLocalRotationX(this Transform self, float x)
        {
            var localRotation = self.localRotation.eulerAngles;
            localRotation.x = x;
            self.rotation = Quaternion.Euler(localRotation);
            return self;
        }

        public static Transform SetLocalRotationY(this Transform self, float y)
        {
            var localRotation = self.localRotation.eulerAngles;
            localRotation.y = y;
            self.rotation = Quaternion.Euler(localRotation);
            return self;
        }

        public static Transform SetLocalRotationZ(this Transform self, float z)
        {
            var localRotation = self.localRotation.eulerAngles;
            localRotation.z = z;
            self.rotation = Quaternion.Euler(localRotation);
            return self;
        }

        #endregion

        #region Transform scale

        public static Transform SetScale(this Transform self, float scale)
        {
            self.localScale = new Vector3(scale, scale, scale);
            return self;
        }

        public static Transform SetScaleX(this Transform self, float x)
        {
            var scale = self.localScale;
            scale.x = x;
            self.localScale = scale;
            return self;
        }

        public static Transform SetScaleY(this Transform self, float y)
        {
            var scale = self.localScale;
            scale.y = y;
            self.localScale = scale;
            return self;
        }

        public static Transform SetScaleZ(this Transform self, float z)
        {
            var scale = self.localScale;
            scale.z = z;
            self.localScale = scale;
            return self;
        }

        #endregion

        #region RectTransform

        public static RectTransform SetSize(this RectTransform self, Vector2 size)
        {
            self.sizeDelta = size;
            return self;
        }
        public static RectTransform SetSizeX(this RectTransform self, float x)
        {
            Vector2 size = self.sizeDelta;
            size.x = x;
            self.sizeDelta = size;
            return self;
        }

        public static RectTransform SetSizeY(this RectTransform self, float y)
        {
            Vector2 size = self.sizeDelta;
            size.y = y;
            self.sizeDelta = size;
            return self;
        }

        public static RectTransform SetAnchorPositionX(this RectTransform self, float x)
        {
            Vector2 position = self.anchoredPosition;
            position.x = x;
            self.anchoredPosition = position;
            return self;
        }

        public static RectTransform SetAnchorPositionY(this RectTransform self, float y)
        {
            Vector2 position = self.anchoredPosition;
            position.y = y;
            self.anchoredPosition = position;
            return self;
        }

        #endregion
    }
}