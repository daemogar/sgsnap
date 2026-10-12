namespace SGSnap.Tests;

public class SnapshotClaimsTests
{
	[Fact]
	public void Each_folder_and_prefix_can_be_claimed_once()
	{
		var claims = new SnapshotClaims();

		claims.Claim("dir", "C.M", "test-1");
		claims.Claim("dir", "C.N", "test-1");
		claims.Claim("other", "C.M", "test-2");
	}

	[Fact]
	public void A_second_claim_by_the_same_test_names_repeated_calls()
	{
		var claims = new SnapshotClaims();
		claims.Claim("dir", "C.M", "test-1");

		var error = Assert.Throws<InvalidOperationException>(() => claims.Claim("dir", "C.M", "test-1"));

		Assert.Contains("more than once", error.Message);
	}

	[Fact]
	public void A_claim_by_another_test_names_theory_rows()
	{
		var claims = new SnapshotClaims();
		claims.Claim("dir", "C.M", "test-1");

		var error = Assert.Throws<InvalidOperationException>(() => claims.Claim("dir", "C.M", "test-2"));

		Assert.Contains("[Theory]", error.Message);
	}
}
