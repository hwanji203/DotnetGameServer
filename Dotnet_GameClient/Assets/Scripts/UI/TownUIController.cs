using CoreSystem;

namespace UI
{
    public class TownUIController : AbstractUIScreenController
    {
        protected override void Bind()
        {
            Btn("enter-dungeon-btn").clicked += () => SceneRouter.Go(SceneRouter.DungeonScene);
            Btn("leave-btn").clicked += () => SceneRouter.Go(SceneRouter.MainScene);
        }
    }
}
