// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    public class AutoRotate : MonoBehaviour
    {
        public Vector3 eulers = new Vector3(0, 90, 0);
        public Space space = Space.World;
        public enum UpdateMode
        {
            Update,
            FixedUpdate,
        }
        [SerializeField]
        private UpdateMode updateMode = UpdateMode.Update;

        [SerializeField]
        [Range(0.1f, 5.0f)]
        private float interval = 2.0f;

        public bool useSin = true;


        private float time = 0;

        protected void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
                UpdatePosition(Time.fixedDeltaTime);
        }

        protected void Update()
        {
            if (updateMode == UpdateMode.Update)
                UpdatePosition(Time.deltaTime);
        }

        void UpdatePosition(float dtime)
        {
            if (useSin)
            {
                time += dtime;
                float ang = (time % interval) / interval * Mathf.PI * 2.0f;
                var t = Mathf.Sin(ang);
                if (space == Space.World)
                    transform.eulerAngles = eulers * t;
                else
                    transform.localEulerAngles = eulers * t;
            }
            else
            {
                transform.Rotate(eulers * dtime, space);
            }
        }
    }

}
