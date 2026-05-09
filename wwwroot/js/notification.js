document.addEventListener("DOMContentLoaded", function () {
    const toast = document.getElementById("app-toast");
    if (!toast) return;

    // Auto remove after animation
    setTimeout(() => {
        toast.remove();
    }, 4000);

    // Click to close
    toast.addEventListener("click", () => {
        toast.remove();
    });
});