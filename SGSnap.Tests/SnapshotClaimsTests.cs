namespace SGSnap.Tests;

public class SnapshotClaimsTests
{
	[Fact]
	public void Each_folder_and_prefix_can_be_claimed_once()
	{
		var claims = new SnapshotClaims();

		claims.Claim("dir", "C.M", "C.M", "test-1");
		claims.Claim("dir", "C.N", "C.N", "test-1");
		claims.Claim("other", "C.M", "C.M", "test-2");
		claims.Claim("whole", null, "C.M", "test-1");
	}

	[Fact]
	public void A_second_claim_by_the_same_test_names_repeated_calls()
	{
		var claims = new SnapshotClaims();
		claims.Claim("dir", "C.M", "C.M", "test-1");

		var error = Assert.Throws<InvalidOperationException>(() => claims.Claim("dir", "C.M", "C.M", "test-1"));

		Assert.Contains("more than once", error.Message);
	}

	[Fact]
	public void A_claim_by_another_test_names_theory_rows()
	{
		var claims = new SnapshotClaims();
		claims.Claim("dir", "C.M", "C.M", "test-1");

		var error = Assert.Throws<InvalidOperationException>(() => claims.Claim("dir", "C.M", "C.M", "test-2"));

		Assert.Contains("[Theory]", error.Message);
	}

	[Fact]
	public void A_whole_folder_claim_admits_no_second_test()
	{
		var claims = new SnapshotClaims();
		claims.Claim("dir", null, "C.M", "test-1");

		var error = Assert.Throws<InvalidOperationException>(() => claims.Claim("dir", null, "C.M", "test-2"));

		Assert.Contains("[Theory]", error.Message);
	}

	[Fact]
	public void Whole_folder_and_prefixed_claims_never_share_a_folder()
	{
		var claims = new SnapshotClaims();
		claims.Claim("a", null, "C.M", "test-1");
		claims.Claim("b", "C.N", "C.N", "test-2");

		Assert.Contains("folder of its own", Assert.Throws<InvalidOperationException>(() => claims.Claim("a", "C.N", "C.N", "test-2")).Message);
		Assert.Contains("folder of its own", Assert.Throws<InvalidOperationException>(() => claims.Claim("b", null, "C.M", "test-1")).Message);
	}
}
