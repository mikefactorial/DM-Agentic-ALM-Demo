/** @type {import('jest').Config} */
module.exports = {
    preset: "ts-jest",
    testEnvironment: "jest-environment-jsdom",
    roots: ["<rootDir>/tests/"],
    globals: {
        "ts-jest": {
            tsconfig: "tsconfig.test.json",
        },
    },
};
