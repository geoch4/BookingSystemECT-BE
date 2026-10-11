using BookingSystem.Application.Authorization;

namespace BookingSystem.Test;

public class AccessRolesTests
{
    [Test]
    public void Admin_Is_Both_Admin_And_Treatment_Unit()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AccessRoles.IsAdmin("Admin"), Is.True);
            Assert.That(AccessRoles.IsTreatmentUnit("Admin"), Is.True);
            Assert.That(AccessRoles.IsReferralDecisionUser("Admin"), Is.True);
        });
    }

    [Test]
    public void Medical_Responsible_Can_Handle_Referral_Decisions_Without_Treatment_Unit_Access()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AccessRoles.IsMedicalResponsible("Medicinskt ansvarig"), Is.True);
            Assert.That(AccessRoles.IsReferralDecisionUser("Medicinskt ansvarig"), Is.True);
            Assert.That(AccessRoles.IsTreatmentUnit("Medicinskt ansvarig"), Is.False);
        });
    }

    [Test]
    public void Department_And_Mottagning_Are_Department_Users()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AccessRoles.IsDepartmentUser("Avdelning"), Is.True);
            Assert.That(AccessRoles.IsDepartmentUser("Vårdavdelning"), Is.True);
            Assert.That(AccessRoles.IsDepartmentUser("Mottagning"), Is.True);
            Assert.That(AccessRoles.IsReferralDecisionUser("Mottagning"), Is.False);
        });
    }
}
