// Run with Unity eval_file only on an explicit in-memory "ime-test-only" account fixture.
// Exercises committed text events; a physical Windows Korean IME still needs manual verification.
var account = UnityEngine.Object.FindAnyObjectByType<DigitalArena.ArenaAccountClient>();
if (account?.Profile?.playerId != "ime-test-only")
    throw new System.Exception("Use a disposable IME fixture; never modify a real account for this test.");
var document = UnityEngine.Object.FindAnyObjectByType<UnityEngine.UIElements.UIDocument>();
var field = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.TextField>(document.rootVisualElement, "nickname");
int checks = 0;
void Check(bool value, string description)
{
    if (!value) throw new System.Exception(description);
    checks++;
}
void Type(string value)
{
    foreach (char c in value)
        using (var e = UnityEngine.UIElements.KeyDownEvent.GetPooled(c, UnityEngine.KeyCode.None, UnityEngine.EventModifiers.None))
            field.SendEvent(e);
}
field.value = "";
field.Focus();
Type("한글테이머A_12");
Check(field.value == "한글테이머A_12", "Committed Hangul and Latin characters must append without losing earlier syllables.");
Check(field.focusController.focusedElement == field, "Editing must retain focus.");
field.SelectRange(2, 2);
Type("새");
Check(field.value == "한글새테이머A_12", "Insertion must preserve text on both sides of the caret.");
using (var e = UnityEngine.UIElements.KeyDownEvent.GetPooled(UnityEngine.Event.KeyboardEvent("backspace")))
    field.SendEvent(e);
Check(field.value == "한글테이머A_12", "Backspace must remove one committed syllable.");
field.SelectAll();
Type("가나다라마바사아자차카타파하");
Check(field.value == "가나다라마바사아자차카타", "Length limit must retain exactly twelve committed Hangul syllables.");
field.Blur();
Check(field.value == "가나다라마바사아자차카타", "Blur must preserve committed text.");
Check(!account.NicknameAvailable && account.CheckedNickname == null, "Editing must invalidate prior nickname availability.");
return "NICKNAME INPUT PASSED: " + checks + " checks (committed text events; OS IME not simulated)";
