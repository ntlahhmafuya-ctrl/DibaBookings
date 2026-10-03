export const clearSession = () => {
    ["token", "userId", "firstName", "lastName", "email", "role"].forEach(
        (key) => localStorage.removeItem(key)
    );

    window.location.href = "/login";
};
