window.blazorHelpers = {
    registerResizeCallback: function (dotnetHelper) {
        function reportWindowSize() {
            dotnetHelper.invokeMethodAsync('HandleWindowResize',
                window.innerWidth, window.innerHeight);
        }

        window.addEventListener('resize', reportWindowSize);
        // Initial call to set starting size
        reportWindowSize();

        // Cleanup function
        return function () {
            window.removeEventListener('resize', reportWindowSize);
        };
    }
};
