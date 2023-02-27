using System.Collections;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    [RequireComponent(typeof(CanvasGroup))]
    public class EnchantBonus : MonoBehaviour
    {
        public float Delay = 1.0f;
        public float FadeTime = 1.0f;

        private CanvasGroup _group;
        private Coroutine _coroutine;

        public void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0;
        }

        public void OnEnable()
        {
            _group.alpha = 0;
        }

        public void OnDestroy()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);
        }

        public IEnumerator FadeOut()
        {
            yield return new WaitForSeconds(Delay);
            while (_group.alpha > 0)
            {
                _group.alpha -= Time.deltaTime * ( 1 / FadeTime );
                yield return null;
            }
        }

        public void Show()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _group.alpha = 1;
            _coroutine = StartCoroutine(FadeOut());
        }
    }
}
