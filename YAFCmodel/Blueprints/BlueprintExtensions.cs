using YAFC.Model;
using System;
using System.Text.Json;
using static YAFC.Blueprints.BlueprintUtilities;
using System.Linq;

namespace YAFC.Blueprints;
public partial class Blueprint {
        public Blueprint AttachBlueprint(Blueprint other, BlueprintAttachDirection attachDirection) {
            var result = JsonSerializer.Deserialize<Blueprint>(JsonSerializer.Serialize(this));
            var highest_index = result.entities.Max(e => e.index);

            // find the leftmost, rightmost, topmost and bottom-most entity
            var leftmost_this = result.entities.Where(e => e.position.x == result.entities.Min(e => e.position.x)).First();
            var rightmost_this = result.entities.Where(e => e.position.x == result.entities.Max(e => e.position.x)).First();
            var topmost_this = result.entities.Where(e => e.position.y == result.entities.Min(e => e.position.y)).First();
            var bottommost_this = result.entities.Where(e => e.position.y == result.entities.Max(e => e.position.y)).First();

            var leftmost_other = other.entities.Where(e => e.position.x == other.entities.Min(e => e.position.x)).First();
            var rightmost_other = other.entities.Where(e => e.position.x == other.entities.Max(e => e.position.x)).First();
            var topmost_other = other.entities.Where(e => e.position.y == other.entities.Min(e => e.position.y)).First();
            var bottommost_other = other.entities.Where(e => e.position.y == other.entities.Max(e => e.position.y)).First();

            var y_span_this = bottommost_this.position.y - topmost_this.position.y;
            var x_span_this = rightmost_this.position.x - leftmost_this.position.x;
            
            var y_span_other = bottommost_other.position.y - topmost_other.position.y;
            var x_span_other = rightmost_other.position.x - leftmost_other.position.x;


            var translationVector = default(BlueprintPosition);


            switch (attachDirection) {
                case BlueprintAttachDirection.Down:
                    translationVector = BlueprintPosition.FromXY(leftmost_this.position.x, bottommost_this.position.y + 1) - BlueprintPosition.FromXY(leftmost_other.position.x, topmost_other.position.y);
                    break;
                default:
                throw new NotImplementedException("only AttachDirection.Down is implemented");
            }

            foreach (var entity in other.entities) {
                result.entities.Add(entity with { position = entity.position + translationVector, index = ++highest_index });
            }
           


            return result;
        }
}