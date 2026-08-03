package com.bide.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.navigation.compose.rememberNavController
import com.bide.app.data.api.RetrofitClient
import com.bide.app.data.api.TokenManager
import com.bide.app.navigation.NavGraph
import com.bide.app.navigation.Routes
import com.bide.app.ui.theme.BIDEappTheme
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val tokenManager = TokenManager(applicationContext)
        val apiService = RetrofitClient.create(tokenManager)

        // Determine start destination based on saved session
        val startDestination = runBlocking {
            val token = tokenManager.token.first()
            val role = tokenManager.role.first()
            if (token != null && role != null) {
                when (role) {
                    "Student" -> Routes.STUDENT_HOME
                    "Instructor" -> Routes.INSTRUCTOR_DASHBOARD
                    "Admin" -> Routes.ADMIN_DASHBOARD
                    else -> Routes.LOGIN
                }
            } else {
                Routes.LOGIN
            }
        }

        setContent {
            BIDEappTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    val navController = rememberNavController()
                    NavGraph(
                        navController = navController,
                        apiService = apiService,
                        tokenManager = tokenManager,
                        startDestination = startDestination
                    )
                }
            }
        }
    }
}
