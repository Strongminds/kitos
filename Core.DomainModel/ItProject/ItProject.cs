using System.Collections.Generic;

namespace Core.DomainModel.ItProject
{
    public class ItProject : IEntity<int>, IHasRights<ItProjectRight>
    {
        public ItProject()
        {
            this.Communications = new List<Communication>();
            this.Economies = new List<Economy>();
            this.ExtReferences = new List<ExtReference>();
            this.TaskRefs = new List<TaskRef>();
            this.Resources = new List<Resource>();
            this.Risks = new List<Risk>();
            this.Stakeholders = new List<Stakeholder>();
            this.Rights = new List<ItProjectRight>();
        }

        public int Id { get; set; }
        public string Background { get; set; }
        public bool IsTransversal { get; set; }
        public bool IsTermsOfReferenceApproved { get; set; }
        public string Note { get; set; }
        public string Name { get; set; }
        public int ProjectType_Id { get; set; }
        public int ProjectCategory_Id { get; set; }
        public int Municipality_Id { get; set; }
        public int? ItProjectOwner_Id { get; set; }
        public int? ItProjectLeader_Id { get; set; }
        public int? PartItProjectLeader_Id { get; set; }
        public int? Consultant_Id { get; set; }

        public virtual ICollection<Communication> Communications { get; set; }
        public virtual ICollection<Economy> Economies { get; set; }
        public virtual ICollection<ExtReference> ExtReferences { get; set; } // TODO
        public virtual GoalStatus GoalStatus { get; set; }
        public virtual Handover Handover { get; set; }
        public virtual ICollection<TaskRef> TaskRefs { get; set; } // TODO
        public virtual OrgTab OrgTab { get; set; }
        public virtual PreAnalysis PreAnalysis { get; set; }
        public virtual ProjectStatus ProjectStatus { get; set; }
        public virtual ICollection<Resource> Resources { get; set; }
        public virtual ICollection<Risk> Risks { get; set; }
        public virtual ICollection<Stakeholder> Stakeholders { get; set; }
        public virtual Organization Organization { get; set; }
        public virtual ProjectCategory ProjectCategory { get; set; }
        public virtual ProjectType ProjectType { get; set; }
        public virtual Hierarchy Hierarchy { get; set; }

        public virtual ICollection<ItProjectRight> Rights { get; set; }
    }
}
