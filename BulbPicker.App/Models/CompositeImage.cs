using System.Drawing;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.Models
{
    public class CompositeImageFragment
    {
        public int FragmentIndex { get; init; }
        public Bitmap Bitmap { get; init; }
        public CompositeImageFragment(int fragmentIndex, Bitmap bitMap)
        {
            FragmentIndex = fragmentIndex;
            Bitmap = bitMap;
        }
    }

    public class CompositeImage
    {
        public bool IsAllFragmentCollected { get; private set; } = false;

        public int Index { get; private set; }

        public CompositeImageFragment? OutsideBefore { get; private set; } = null;
        public CompositeImageFragment? OutsideAfter { get; private set; } = null;

        public CompositeImageFragment? InsideBefore { get; private set; } = null;
        public CompositeImageFragment? InsideAfter { get; private set; } = null;


        /*
         * [Composite Image Example]
         * 
         *  [0th][0th] ------ Before
         *  [1st][1st] ------ After
         *
         *  [1st][1st] ------ Before
         *  [2nd][2nd] ------ After
         *
         *  [2nd][2nd] ------ Before
         *  [3rd][3rd] ------ After
         */

        public void AddImage(int newIndex, BaslerCameraPosition position, Bitmap newBitmap)
        {
            // 소유권 문제로 new
            Bitmap newOwnedBitmap = new Bitmap(newBitmap);

            CompositeImageFragment newFragment = new CompositeImageFragment(newIndex, newOwnedBitmap);
            
            // First 2 Rows of Images
            if(newIndex < 2)
            {
                if(newIndex == 0)
                {
                    if (position == BaslerCameraPosition.Outisde) OutsideBefore = newFragment;
                    else InsideBefore = newFragment;
                }
                else
                {
                    if (position == BaslerCameraPosition.Outisde) OutsideAfter = newFragment;
                    else InsideAfter = newFragment;

                    IsAllFragmentCollected = (OutsideAfter != null && InsideAfter != null);
                }
                return;
            }

            // Outside
            if (position == BaslerCameraPosition.Outisde)
            {
                OutsideBefore.Bitmap.Dispose();

                OutsideBefore = OutsideAfter;
                OutsideAfter = newFragment;
            }
            // Inside
            else
            {
                InsideBefore.Bitmap.Dispose();

                InsideBefore = InsideAfter;
                InsideAfter = newFragment;
            }

            IsAllFragmentCollected = (OutsideAfter.FragmentIndex == InsideAfter.FragmentIndex);
        }
    }
}
