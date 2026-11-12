namespace ShiftShift.Core
{
    /// <summary>What a menu understands from the two Shift keys.</summary>
    public enum MenuKey { None, Left, Right, Both }

    /// <summary>
    /// Menu navigation with only Left and Right Shift (menus, results, pause): one key on its own = Left or Right,
    /// reported when it is let go; both down at once = Both, reported the moment the second goes down. After a Both,
    /// nothing more is reported until every key is up, so letting go of the two keys never counts as a Left or Right.
    /// Feed it the key states once per frame. No timing involved.
    /// </summary>
    public sealed class ShiftMenuKeys
    {
        private bool _bothFired;
        private bool _sawLeft, _sawRight; // pressed since the keys were last all up

        public MenuKey Poll(bool leftDown, bool rightDown)
        {
            MenuKey result = MenuKey.None;
            if (leftDown) _sawLeft = true;
            if (rightDown) _sawRight = true;

            if (leftDown && rightDown && !_bothFired)
            {
                _bothFired = true;
                result = MenuKey.Both;
            }
            else if (!leftDown && !rightDown)
            {
                if (!_bothFired)
                {
                    // Pressed one after the other within one frame's poll, or one alone: the one seen wins.
                    if (_sawLeft && !_sawRight) result = MenuKey.Left;
                    else if (_sawRight && !_sawLeft) result = MenuKey.Right;
                    else if (_sawLeft && _sawRight) result = MenuKey.Both; // both went down and up without overlapping polls
                }
                _bothFired = false;
                _sawLeft = _sawRight = false;
            }
            return result;
        }

        /// <summary>Forget everything (scene start: a key held from the previous screen must not count).</summary>
        public void Reset(bool leftDown, bool rightDown)
        {
            _sawLeft = _sawRight = false;
            _bothFired = leftDown || rightDown; // wait for all keys up before reporting anything
        }
    }
}
