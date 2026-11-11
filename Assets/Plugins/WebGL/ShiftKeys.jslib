// Shift+Shift: reads the two Shift keys straight from the browser (day 17).
// Why: on Windows, when both Shift keys are down and one is let go, the browser gets no key-up for it. Unity's own
// keyboard state then shows that key as held for good, and its next press is lost. Here every Shift key-up checks
// whether ANY Shift is still down (event.shiftKey); if none is, both are cleared.
// A fresh key-down for a key still marked as down means its key-up was one of the lost ones: reported as "again".
// State bits: 1 left down, 2 right down, 4 left pressed since the last read, 8 right pressed since the last read,
// 16 left pressed again while marked down, 32 right pressed again while marked down.
mergeInto(LibraryManager.library, {
  ShiftKeys_Init: function () {
    if (Module.shiftKeys) return;
    var s = Module.shiftKeys = { left: false, right: false, leftHit: false, rightHit: false, leftAgain: false, rightAgain: false };
    window.addEventListener('keydown', function (e) {
      if (e.repeat) return;
      if (e.code === 'ShiftLeft') { if (s.left) s.leftAgain = true; s.left = true; s.leftHit = true; }
      else if (e.code === 'ShiftRight') { if (s.right) s.rightAgain = true; s.right = true; s.rightHit = true; }
    }, true);
    window.addEventListener('keyup', function (e) {
      if (e.code === 'ShiftLeft') s.left = false;
      else if (e.code === 'ShiftRight') s.right = false;
      if (e.key === 'Shift' && !e.shiftKey) { s.left = false; s.right = false; }
    }, true);
    window.addEventListener('blur', function () { s.left = false; s.right = false; });
  },
  ShiftKeys_State: function () {
    var s = Module.shiftKeys;
    if (!s) return 0;
    var bits = (s.left ? 1 : 0) | (s.right ? 2 : 0) | (s.leftHit ? 4 : 0) | (s.rightHit ? 8 : 0) |
      (s.leftAgain ? 16 : 0) | (s.rightAgain ? 32 : 0);
    s.leftHit = s.rightHit = s.leftAgain = s.rightAgain = false;
    return bits;
  }
});
