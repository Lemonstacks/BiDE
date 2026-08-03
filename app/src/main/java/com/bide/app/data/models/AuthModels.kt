package com.bide.app.data.models

data class LoginRequest(
    val email: String,
    val password: String
)

data class RegisterRequest(
    val firstName: String,
    val lastName: String,
    val email: String,
    val contact: String,
    val password: String,
    val confirmPassword: String,
    val role: String,
    val suburb: String? = null
)

data class AuthResponse(
    val token: String,
    val role: String,
    val userId: Int,
    val name: String,
    val email: String
)
