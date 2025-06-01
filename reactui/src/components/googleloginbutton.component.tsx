
function GoogleLoginButton() {
  const handleClick = async () => {
    try {
      window.location.href = '/api/google-login';
    } catch (error) {
      console.error('Error during Google authentication:', error);
    }
  }

  return (
    <button onClick={handleClick}>Login using Google</button>
  );
}

export default GoogleLoginButton;