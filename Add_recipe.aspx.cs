using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace sdaproject
{
    public partial class Add_recipe : System.Web.UI.Page
    {
       
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!SessionManager.Instance.IsLoggedIn())
            {
                Response.Redirect("Login.aspx");
                return;
            }
            if (!IsPostBack)
            {
                LoadCategories();
            }

        }

        protected void AddRecipeBtn_Click(object sender, EventArgs e)
        {

            int userId = SessionManager.Instance.UserId; // assumes your session has this method
            string name = RecipeNameTxt.Text;
            string categoryId = CategoryDropdown.SelectedValue;
            string ingredients = IngredientsTxt.Text;
            string steps = StepsTxt.Text;

            // Check if any input field is empty
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(categoryId) ||
                string.IsNullOrWhiteSpace(ingredients) ||
                string.IsNullOrWhiteSpace(steps))
            {
                ShowAlert("All fields are required.");
                return;
            }

            IRecipeFactory factory = new RecipeFactory();
            Recipe recipe = factory.CreateRecipe(userId, name, categoryId, ingredients, steps);

            conn db = new conn();
            SqlConnection con = db.GetConnection();

            string query = @"INSERT INTO Recipes (UserId, RecipeName, CategoryId, Ingredients, StepsToCook, DateCreated)
VALUES (@UserId, @RecipeName, @CategoryId, @Ingredients, @StepsToCook, @DateCreated)";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@UserId", recipe.UserId);
            cmd.Parameters.AddWithValue("@RecipeName", recipe.RecipeName);
            cmd.Parameters.AddWithValue("@CategoryId", categoryId);
            cmd.Parameters.AddWithValue("@Ingredients", recipe.Ingredients);
            cmd.Parameters.AddWithValue("@StepsToCook", recipe.StepsToCook);
            cmd.Parameters.AddWithValue("@DateCreated", recipe.DateCreated);

            try
            {
                con.Open();
                cmd.ExecuteNonQuery();

                SqlCommand pointcmd = new SqlCommand("UpdateUserPointsAndBadges", con);
                pointcmd.CommandType = System.Data.CommandType.StoredProcedure;
                pointcmd.Parameters.AddWithValue("@UserId", userId);
                pointcmd.ExecuteNonQuery();

                UserProfileSubject subject = new UserProfileSubject();
                EmailNotifierObserver observer = new EmailNotifierObserver(ObserverLabel);
                subject.Attach(observer);
                subject.Notify("Recipe added successfully!");
                subject.Detach(observer);

                RecipeNameTxt.Text = "";
                CategoryDropdown.ClearSelection();
                IngredientsTxt.Text = "";
                StepsTxt.Text = "";
            }
            catch (Exception ex)
            {
                ShowAlert("Error: " + ex.Message);
            }
            finally
            {
                con.Close();
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Response.Redirect("My_Recipes.aspx");
        }

        private void LoadCategories()
        {
            conn db = new conn();
            SqlConnection con = db.GetConnection();

            string query = "SELECT CategoryId, CategoryName FROM Category";
            SqlCommand cmd = new SqlCommand(query, con);

            try
            {
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                CategoryDropdown.DataSource = reader;
                CategoryDropdown.DataTextField = "CategoryName";
                CategoryDropdown.DataValueField = "CategoryId";
                CategoryDropdown.DataBind();
            }
            catch (Exception ex)
            {
                ShowAlert("Error loading recipes: " + ex.Message);
            }
            finally
            {
                con.Close();
            }
        }
        private void ShowAlert(string message)
        {
            string script = $"alert('{message.Replace("'", "\\'")}');";
            ClientScript.RegisterStartupScript(this.GetType(), "alert", script, true);
        }


    }

}