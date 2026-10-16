using UnityEngine;

namespace CivilGame.Book
{
    [CreateAssetMenu(fileName = "NewManualPage", menuName = "CivilGame/Manual Page Data")]
    public class ManualPageData : ScriptableObject
    {
        [Header("Left Page")]
        [SerializeField] private string _leftPageTitle;
        [SerializeField] private Sprite _leftPageIllustration;
        [TextArea(4, 10)]
        [SerializeField] private string _leftPageContent;

        [Header("Right Page")]
        [SerializeField] private string _rightPageTitle;
        [SerializeField] private Sprite _rightPageIllustration;
        [TextArea(4, 10)]
        [SerializeField] private string _rightPageContent;

        [Header("Footer info")]
        [SerializeField] private string _pageNumberText;

        public string LeftPageTitle => _leftPageTitle;
        public Sprite LeftPageIllustration => _leftPageIllustration;
        public string LeftPageContent => _leftPageContent;

        public string RightPageTitle => _rightPageTitle;
        public Sprite RightPageIllustration => _rightPageIllustration;
        public string RightPageContent => _rightPageContent;

        public string PageNumberText => _pageNumberText;
    }
}
